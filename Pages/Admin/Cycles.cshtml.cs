// /Pages/Admin/Cycles.cshtml.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AppraisalPortal.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CyclesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public CyclesModel(ApplicationDbContext db) { _db = db; }

        [BindProperty] public string Name { get; set; } = $"H2 {DateTime.Now.Year}";
        [BindProperty] public DateTime Start { get; set; } = DateTime.Today.AddDays(-7);
        [BindProperty] public DateTime End { get; set; } = DateTime.Today.AddMonths(1);

        public IList<AppraisalCycle> Cycles { get; set; } = new List<AppraisalCycle>();

        // simple per-cycle counts for the grid
        public Dictionary<int, (int Total, int Submitted, int MgrReviewed)> Counts { get; set; } = new();

        public async Task OnGetAsync()
        {
            Cycles = await _db.AppraisalCycles
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            var ids = Cycles.Select(c => c.Id).ToList();

            var totals = await _db.Forms
                .Where(f => ids.Contains(f.CycleId))
                .GroupBy(f => f.CycleId)
                .Select(g => new { CycleId = g.Key, Total = g.Count() })
                .ToListAsync();

            var submitted = await _db.Forms
                .Where(f => ids.Contains(f.CycleId) && f.Status == "Submitted")
                .GroupBy(f => f.CycleId)
                .Select(g => new { CycleId = g.Key, Count = g.Count() })
                .ToListAsync();

            var reviewed = await _db.Forms
                .Where(f => ids.Contains(f.CycleId) && f.Status == "MgrReviewed")
                .GroupBy(f => f.CycleId)
                .Select(g => new { CycleId = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var id in ids)
            {
                var t = totals.FirstOrDefault(x => x.CycleId == id)?.Total ?? 0;
                var s = submitted.FirstOrDefault(x => x.CycleId == id)?.Count ?? 0;
                var m = reviewed.FirstOrDefault(x => x.CycleId == id)?.Count ?? 0;
                Counts[id] = (t, s, m);
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            _db.AppraisalCycles.Add(new AppraisalCycle { Name = Name, Start = Start, End = End, Status = "Open" });

            // Seed default questions only once (legacy self/manager Q&A)
            if (!await _db.Questions.AnyAsync())
            {
                _db.Questions.AddRange(
                    new Question { Section = "KPIs", Text = "Quality of work", Weight = 2 },
                    new Question { Section = "KPIs", Text = "Timeliness / deadlines", Weight = 2 },
                    new Question { Section = "Behavior", Text = "Teamwork and collaboration", Weight = 1 },
                    new Question { Section = "Behavior", Text = "Communication", Weight = 1 }
                );
            }

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Cycle created. Default questions prepared.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostGenerateFormsAsync(int id)
        {
            var qs = await _db.Questions.AsNoTracking().ToListAsync();
            var emps = await _db.Employees.AsNoTracking().ToListAsync();

            foreach (var e in emps)
            {
                if (await _db.Forms.AnyAsync(f => f.EmployeeId == e.Id && f.CycleId == id))
                    continue;

                var form = new Form { EmployeeId = e.Id, CycleId = id, Status = "Draft" };

                // legacy responses so the old pages still work; manager/employee new pages ignore them
                foreach (var q in qs)
                    form.Responses.Add(new Response { QuestionId = q.Id });

                _db.Forms.Add(form);
            }

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Forms generated for all employees.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            // gather forms of the cycle
            var forms = await _db.Forms.Where(f => f.CycleId == id).ToListAsync();
            var formIds = forms.Select(f => f.Id).ToList();

            // remove associated evaluation data
            _db.Responsibilities.RemoveRange(_db.Responsibilities.Where(r => formIds.Contains(r.FormId)));
            _db.KPIItems.RemoveRange(_db.KPIItems.Where(k => formIds.Contains(k.FormId)));
            _db.SoftSkillRatings.RemoveRange(_db.SoftSkillRatings.Where(s => formIds.Contains(s.FormId)));
            _db.Responses.RemoveRange(_db.Responses.Where(r => formIds.Contains(r.FormId)));

            _db.Forms.RemoveRange(forms);

            var cycle = await _db.AppraisalCycles.FindAsync(id);
            if (cycle != null) _db.AppraisalCycles.Remove(cycle);

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Cycle and all related data were deleted.";
            return RedirectToPage();
        }
    }
}
