// /Pages/Employee/Edit.cshtml.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AppraisalPortal.Pages.EmployeePages
{
    [Authorize(Roles = "Employee")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public EditModel(ApplicationDbContext db) { _db = db; }
        [BindProperty] public Form Form { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Form = await _db.Forms.Include(f => f.Responses).ThenInclude(r => r.Question)
                                  .FirstAsync(f => f.Id == id);
            if (Form.Status != "Draft" && Form.Status != "Submitted") return Forbid();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync(int id)
        {
            var form = await _db.Forms.Include(f => f.Responses).FirstAsync(f => f.Id == id);
            foreach (var r in form.Responses)
            {
                int.TryParse(Request.Form[$"self_{r.Id}"], out var sr);
                r.SelfRating = sr == 0 ? null : sr;
                r.SelfComment = Request.Form[$"selfc_{r.Id}"];
            }
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
