// /Pages/Manager/Review.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AppraisalPortal.Pages.ManagerPages
{
    [Authorize(Roles = "Manager,Admin")]
    public class ReviewModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ScoringService _score;

        public ReviewModel(ApplicationDbContext db, ScoringService score)
        {
            _db = db; _score = score;
        }

        public int FormId { get; set; }
        public string EmployeeName { get; set; } = "";
        public string CycleName { get; set; } = "";
        [BindProperty] public string? ManagerComments { get; set; }

        // posted models
        public class KpiInput { public string? Description { get; set; } public string? Actual { get; set; } public int? Score { get; set; } }
        public class SoftInput { public string Attribute { get; set; } = ""; public int? Score { get; set; } }

        [BindProperty] public List<KpiInput> KPI { get; set; } = new();
        [BindProperty] public List<SoftInput> Soft { get; set; } = new();

        // for internal use (optional server rendering)
        public class SelfRow { public string Title { get; set; } = ""; public string? Description { get; set; } public int Achievement { get; set; } }
        public List<SelfRow> SelfItems { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var form = await _db.Forms
                .Include(f => f.Employee)
                .Include(f => f.Cycle)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (form == null) return NotFound();

            FormId = id;
            EmployeeName = form.Employee.Name;
            CycleName = form.Cycle.Name;
            ManagerComments = form.ManagerComments;

            // KPI: load existing; ensure exactly 5 rows (pad empties)
            var kpis = await _db.KPIItems
                .AsNoTracking()
                .Where(x => x.FormId == id)
                .OrderBy(x => x.Id)
                .ToListAsync();

            KPI = kpis.Select(k => new KpiInput
            {
                Description = k.Description,
                Actual = k.ActualPerformance,
                Score = k.Score
            }).ToList();

            while (KPI.Count < 5) KPI.Add(new KpiInput());

            // Soft: ensure 10 default rows exist; then load
            var soft = await _db.SoftSkillRatings
                .Where(x => x.FormId == id)
                .OrderBy(x => x.Id)
                .ToListAsync();

            if (soft.Count == 0)
            {
                var defaults = new[]
                {
                    "Punctuality & Attendance","Attitude & Professionalism","Time Management","Communication Skills","Team Collaboration",
                    "Adaptability & Flexibility","Initiative & Proactiveness","Problem-Solving Ability","Accountability & Ownership","Compliance with Company Values"
                };
                foreach (var a in defaults)
                    _db.SoftSkillRatings.Add(new SoftSkillRating { FormId = id, Attribute = a, Score = 5 });
                await _db.SaveChangesAsync();

                soft = await _db.SoftSkillRatings
                    .Where(x => x.FormId == id)
                    .OrderBy(x => x.Id)
                    .ToListAsync();
            }

            Soft = soft.Select(s => new SoftInput { Attribute = s.Attribute, Score = s.Score }).ToList();

            // Self-eval (server-side copy; UI fetches via AJAX too)
            var resps = await _db.Responsibilities
                .AsNoTracking()
                .Where(r => r.FormId == id)
                .OrderBy(r => r.Id)
                .ToListAsync();

            if (resps.Count > 0)
            {
                SelfItems = resps.Select(r => new SelfRow
                {
                    Title = r.Title ?? string.Empty,
                    Description = r.Description,
                    Achievement = r.AchievementPercent
                }).ToList();
            }
            else
            {
                // Fallback in case older data stored self rows as KPIItems
                var selfKpi = await _db.KPIItems
                    .AsNoTracking()
                    .Where(k => k.FormId == id)
                    .OrderBy(k => k.Id)
                    .ToListAsync();

                if (selfKpi.Count > 0)
                {
                    SelfItems = selfKpi.Select(k => new SelfRow
                    {
                        Title = k.Description ?? string.Empty,
                        Description = k.ActualPerformance,
                        Achievement = 0
                    }).ToList();
                }
            }

            return Page();
        }

        // JSON: used by the modal to show self-eval
        public async Task<IActionResult> OnGetSelfEvalAsync(int id)
        {
            var exists = await _db.Forms.AsNoTracking().AnyAsync(f => f.Id == id);
            if (!exists) return NotFound();

            var resp = await _db.Responsibilities
                .AsNoTracking()
                .Where(r => r.FormId == id)
                .OrderBy(r => r.Id)
                .Select(r => new { title = r.Title ?? "", description = r.Description ?? "", achievementPercent = r.AchievementPercent })
                .ToListAsync();

            if (resp.Count > 0) return new JsonResult(resp);

            var kpi = await _db.KPIItems
                .AsNoTracking()
                .Where(k => k.FormId == id)
                .OrderBy(k => k.Id)
                .Select(k => new { title = k.Description ?? "", description = k.ActualPerformance ?? "", achievementPercent = 0 })
                .ToListAsync();

            return new JsonResult(kpi);
        }

        public async Task<IActionResult> OnPostSaveAsync(int id)
        {
            await PersistAsync(id);
            TempData["Msg"] = "Saved as draft.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostSubmitAsync(int id)
        {
            await PersistAsync(id);
            var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == id);
            if (form != null)
            {
                form.Status = "MgrReviewed";
                await _db.SaveChangesAsync();
                await _score.ComputeAsync(id);
            }
            return RedirectToPage("/Manager/Inbox");
        }

        private async Task PersistAsync(int id)
        {
            // Replace KPI items with EXACTLY 5 rows; clamp and require description/actual
            var existingKpi = _db.KPIItems.Where(x => x.FormId == id);
            _db.KPIItems.RemoveRange(existingKpi);

            // guard against null binding scenarios
            KPI ??= new List<KpiInput>();
            while (KPI.Count < 5) KPI.Add(new KpiInput());

            for (int i = 0; i < 5; i++)
            {
                var k = KPI[i];
                var desc = (k.Description ?? "").Trim();
                var act = (k.Actual ?? "").Trim();
                var score = Math.Clamp(k.Score ?? 0, 0, 100);

                // server-side require too (UI already enforces)
                if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(act))
                    continue;

                _db.KPIItems.Add(new KPIItem
                {
                    FormId = id,
                    Description = desc,
                    ActualPerformance = act,
                    Score = score
                });
            }

            // Update Soft skill scores by attribute (do not change attribute names/order)
            Soft ??= new List<SoftInput>();
            var softRows = await _db.SoftSkillRatings.Where(x => x.FormId == id).ToListAsync();
            foreach (var s in Soft)
            {
                var row = softRows.FirstOrDefault(x => x.Attribute == s.Attribute);
                if (row != null)
                    row.Score = Math.Clamp(s.Score ?? 1, 1, 10);
            }

            // Save manager comments
            var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == id);
            if (form != null)
                form.ManagerComments = ManagerComments;

            // IMPORTANT: we DO NOT touch Responsibilities here. Employee self-eval stays intact.
            await _db.SaveChangesAsync();
        }
    }
}
