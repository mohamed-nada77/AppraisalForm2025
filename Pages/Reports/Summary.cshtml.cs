// /Pages/Reports/Summary.cshtml.cs
using System.Security.Claims;
using AppraisalPortal.Models;           // for Form, Responsibility, KPIItem, SoftSkillRating
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AppraisalPortal.Pages.Reports
{
    [Authorize]
    [AutoValidateAntiforgeryToken] // apply once at PageModel level (fixes MVC1001 warnings)
    public class SummaryModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public SummaryModel(ApplicationDbContext db) => _db = db;

        // -------- Role flags / helpers --------
        public bool IsAdmin => User.IsInRole("Admin");
        public bool IsHR => User.IsInRole("HR");
        public bool IsCEO
        {
            get
            {
                if (User.IsInRole("CEO")) return true;
                var code = CurrentEmployee?.EmpCode?.Trim();
                return code == "7"; // CEO emp code rule
            }
        }

        public string NotFoundMessage { get; set; } = "";

        // -------- Left pane lists --------
        public class SummaryItem
        {
            public int Id { get; set; }
            public string EmpName { get; set; } = "";
            public string EmpCode { get; set; } = "";
            public string CycleName { get; set; } = "";
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Department { get; set; } = "";
        }

        public Dictionary<string, List<SummaryItem>> Awaiting { get; set; } = new();
        public Dictionary<string, List<SummaryItem>> Approved { get; set; } = new();

        // -------- Selected form (right pane) --------
        public AppraisalPortal.Models.Form? Form { get; set; }
        public string? ManagerName { get; set; }

        public class SelfSummaryRow { public string Title { get; set; } = ""; public string? Description { get; set; } public int Achievement { get; set; } }
        public class KpiSummaryRow { public string Description { get; set; } = ""; public string? Actual { get; set; } public int Score { get; set; } }
        public class SoftSummaryRow { public string Attribute { get; set; } = ""; public int Score { get; set; } }

        public List<SelfSummaryRow> SelfRows { get; set; } = new();
        public List<KpiSummaryRow> KpiRows { get; set; } = new();
        public List<SoftSummaryRow> SoftRows { get; set; } = new();

        public int KpiTotal { get; set; }   // /100
        public decimal KpiWeighted { get; set; }   // *0.70
        public int SoftPctTotal { get; set; }   // /100
        public decimal SoftWeighted { get; set; }   // *0.30
        public decimal FinalWeighted { get; set; }   // sum

        public string ShortDate(DateTime dt) => dt.ToString("dd/MM/yy");

        // Fully-qualify the Employee model to avoid conflict with Pages.Employee namespace
        private AppraisalPortal.Models.Employee? _currentEmployee;
        private AppraisalPortal.Models.Employee? CurrentEmployee
        {
            get
            {
                if (_currentEmployee is not null) return _currentEmployee;
                var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(uid)) return null;

                _currentEmployee = _db.Employees
                    .AsNoTracking()
                    .FirstOrDefault(e => e.UserId == uid);

                return _currentEmployee;
            }
        }

        // ========================= GET =========================
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            // Build base query (EXCLUDE employees without reporting manager)
            var baseQuery = _db.Forms
                .AsNoTracking()
                .Include(f => f.Employee)
                .Include(f => f.Cycle)
                .Where(f => f.Employee.ManagerId != null);

            // Awaiting HR approval (MgrReviewed)
            var awaiting = await baseQuery
                .Where(f => f.Status == "MgrReviewed")
                .OrderBy(f => f.Employee.Department).ThenBy(f => f.Employee.Name)
                .Select(f => new SummaryItem
                {
                    Id = f.Id,
                    EmpName = f.Employee.Name,
                    EmpCode = f.Employee.EmpCode,
                    CycleName = f.Cycle.Name,
                    Start = f.Cycle.Start,
                    End = f.Cycle.End,
                    Department = f.Employee.Department ?? "—"
                })
                .ToListAsync();

            Awaiting = awaiting
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Department) ? "—" : x.Department)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Approved (visible to CEO)
            var approved = await baseQuery
                .Where(f => f.Status == "Approved")
                .OrderBy(f => f.Employee.Department).ThenBy(f => f.Employee.Name)
                .Select(f => new SummaryItem
                {
                    Id = f.Id,
                    EmpName = f.Employee.Name,
                    EmpCode = f.Employee.EmpCode,
                    CycleName = f.Cycle.Name,
                    Start = f.Cycle.Start,
                    End = f.Cycle.End,
                    Department = f.Employee.Department ?? "—"
                })
                .ToListAsync();

            Approved = approved
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Department) ? "—" : x.Department)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList());

            // If no specific form requested, render the lists
            if (id is null) return Page();

            // Load the selected form (also exclude no-manager forms)
            Form = await _db.Forms
                .Include(f => f.Employee).ThenInclude(e => e.Manager)
                .Include(f => f.Cycle)
                .FirstOrDefaultAsync(f => f.Id == id && f.Employee.ManagerId != null);

            if (Form is null)
            {
                NotFoundMessage = "Selected form was not found (or employee has no reporting manager).";
                return Page();
            }

            ManagerName = Form.Employee.Manager?.Name ?? Form.Employee.ManagerNameCached;

            // Section 2: Employee self-evaluation (Responsibilities, fallback to KPIItems)
            var resp = await _db.Responsibilities
                .AsNoTracking()
                .Where(r => r.FormId == Form.Id)
                .OrderBy(r => r.Id)
                .ToListAsync();

            if (resp.Count > 0)
            {
                SelfRows = resp.Select(r => new SelfSummaryRow
                {
                    Title = r.Title ?? "",
                    Description = r.Description,
                    Achievement = r.AchievementPercent
                }).ToList();
            }
            else
            {
                var kpiAsSelf = await _db.KPIItems
                    .AsNoTracking()
                    .Where(k => k.FormId == Form.Id)
                    .OrderBy(k => k.Id)
                    .ToListAsync();

                SelfRows = kpiAsSelf.Select(k => new SelfSummaryRow
                {
                    Title = k.Description ?? "",
                    Description = k.ActualPerformance,
                    Achievement = 0
                }).ToList();
            }

            // Section 3: Manager KPI (70%)
            var kpi = await _db.KPIItems
                .AsNoTracking()
                .Where(k => k.FormId == Form.Id)
                .OrderBy(k => k.Id)
                .ToListAsync();

            KpiRows = kpi.Select(k => new KpiSummaryRow
            {
                Description = k.Description ?? "",
                Actual = k.ActualPerformance,
                Score = k.Score
            }).ToList();

            if (KpiRows.Count > 0)
            {
                KpiTotal = (int)Math.Round(KpiRows.Average(x => (double)x.Score));
                KpiWeighted = Math.Round((decimal)KpiTotal * 0.70m, 2);
            }
            else
            {
                KpiTotal = 0; KpiWeighted = 0m;
            }

            // Section 4: Soft skills (30%)
            var soft = await _db.SoftSkillRatings
                .AsNoTracking()
                .Where(s => s.FormId == Form.Id)
                .OrderBy(s => s.Id)
                .ToListAsync();

            SoftRows = soft.Select(s => new SoftSummaryRow
            {
                Attribute = s.Attribute,
                Score = s.Score
            }).ToList();

            if (SoftRows.Count > 0)
            {
                var sum = SoftRows.Sum(x => x.Score); // scores 1..10
                var pct = (int)Math.Round((double)sum / (SoftRows.Count * 10) * 100);
                SoftPctTotal = pct;
                SoftWeighted = Math.Round((decimal)pct * 0.30m, 2);
            }
            else
            {
                SoftPctTotal = 0; SoftWeighted = 0m;
            }

            FinalWeighted = Math.Round(KpiWeighted + SoftWeighted, 2);
            return Page();
        }

        // ========================= POST: HR approve =========================
        public async Task<IActionResult> OnPostHrApproveAsync(int id, string? hrComment)
        {
            // HR Manager (EmpCode 88) OR HR role OR Admin
            var isHrManagerCode = (CurrentEmployee?.EmpCode?.Trim() == "88");
            if (!(IsAdmin || IsHR || isHrManagerCode))
                return Forbid();

            var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == id);
            if (form is null) return NotFound();

            form.HRComments = hrComment;
            form.Status = "Approved";
            await _db.SaveChangesAsync();

            return RedirectToPage(new { id });
        }

        // ========================= POST: CEO comment (optional) =========================
        public async Task<IActionResult> OnPostCeoCommentAsync(int id, string? ceoComment)
        {
            if (!(IsAdmin || IsCEO))
                return Forbid();

            var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == id);
            if (form is null) return NotFound();

            form.CEOComments = ceoComment;
            await _db.SaveChangesAsync();

            return RedirectToPage(new { id });
        }
    }
}
