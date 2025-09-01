// /Pages/Employee/Self.cshtml.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AppraisalPortal.Pages.EmployeePages
{
    [Authorize(Roles = "Employee")]
    public class SelfModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public SelfModel(ApplicationDbContext db) { _db = db; }

        public int FormId { get; set; }
        [BindProperty] public List<Row> Items { get; set; } = new();
        [BindProperty] public string? Comments { get; set; }

        public class Row
        {
            public string? Title { get; set; }
            public string? Description { get; set; }
            public int? Achievement { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var form = await _db.Forms
                .Include(f => f.Employee)
                .Include(f => f.Cycle)
                .FirstAsync(f => f.Id == id);

            if (form.Status != "Draft" && form.Status != "Submitted") return Forbid();

            FormId = id;
            Comments = form.SelfComments;

            var resps = await _db.Responsibilities
                .Where(r => r.FormId == id).OrderBy(r => r.Id).ToListAsync();

            if (resps.Count == 0)
            {
                Items = new List<Row> {
                    new(), new(), new()
                };
            }
            else
            {
                Items = resps.Select(r => new Row { Title = r.Title, Description = r.Description, Achievement = r.AchievementPercent }).ToList();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync(int id)
        {
            var form = await _db.Forms.FindAsync(id);
            if (form == null) return NotFound();

            // replace all responsibilities for simplicity
            var existing = _db.Responsibilities.Where(r => r.FormId == id);
            _db.Responsibilities.RemoveRange(existing);

            foreach (var i in Items.Where(x => !string.IsNullOrWhiteSpace(x.Title) && !string.IsNullOrWhiteSpace(x.Description)))
            {
                _db.Responsibilities.Add(new Responsibility
                {
                    FormId = id,
                    Title = i.Title!.Trim(),
                    Description = i.Description!.Trim(),
                    AchievementPercent = Math.Clamp(i.Achievement ?? 0, 0, 100)
                });
            }

            form.SelfComments = Comments;
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Saved.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostSubmitAsync(int id)
        {
            var form = await _db.Forms.FindAsync(id);
            if (form == null) return NotFound();
            form.Status = "Submitted";
            await _db.SaveChangesAsync();
            return RedirectToPage("/Employee/Appraisals");
        }
    }
}
