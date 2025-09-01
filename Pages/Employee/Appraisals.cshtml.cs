// /Pages/Employee/Appraisals.cshtml.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AppraisalPortal.Pages.EmployeePages
{
    [Authorize(Roles = "Employee,Manager,HR,CEO,Admin")]
    public class MyAppraisalsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _um;
        public MyAppraisalsModel(ApplicationDbContext db, UserManager<AppUser> um) { _db = db; _um = um; }
        public IList<Form> Forms { get; set; } = new List<Form>();

        public async Task OnGetAsync()
        {
            var user = await _um.GetUserAsync(User);
            var emp = await _db.Employees.FirstAsync(e => e.UserId == user!.Id);
            Forms = await _db.Forms.Include(f => f.Cycle)
                                   .Where(f => f.EmployeeId == emp.Id)
                                   .OrderByDescending(f => f.Id).ToListAsync();
        }
    }
}
