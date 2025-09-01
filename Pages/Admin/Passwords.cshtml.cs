using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EmployeeModel = AppraisalPortal.Models.Employee;   // <-- ADD THIS ALIAS

namespace AppraisalPortal.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class PasswordsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly PasswordRuleService _rule;
        public PasswordsModel(ApplicationDbContext db, PasswordRuleService rule) { _db = db; _rule = rule; }

        public IList<EmployeeModel> Items { get; set; } = new List<EmployeeModel>();   // <-- use alias

        public async Task OnGetAsync()
        {
            Items = await _db.Employees.AsNoTracking()
                .OrderBy(e => e.EmpCode)
                .ToListAsync();
        }

        public string Make(EmployeeModel e) => _rule.Generate(e);  // <-- use alias
    }
}
