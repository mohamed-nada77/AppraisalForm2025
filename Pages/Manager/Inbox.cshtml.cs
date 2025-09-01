// /Pages/Manager/Inbox.cshtml.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AppraisalPortal.Pages.ManagerPages
{
    [Authorize(Roles = "Manager,Admin")]
    public class InboxModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _um;
        public InboxModel(ApplicationDbContext db, UserManager<AppUser> um) { _db = db; _um = um; }

        public IList<Form> ToReview { get; set; } = new List<Form>();
        public IList<Form> Drafts { get; set; } = new List<Form>();
        public IList<Form> InReview { get; set; } = new List<Form>();

        public async Task OnGetAsync()
        {
            var user = await _um.GetUserAsync(User);
            var me = await _db.Employees.FirstAsync(e => e.UserId == user!.Id);
            var myCode = (me.EmpCode ?? "").Trim();
            var myName = (me.Name ?? "").Trim();

            // 1) direct team by strong/weak links
            var teamQ = _db.Forms
              .Include(f => f.Employee).Include(f => f.Cycle)
              .Where(f =>
                    f.EmployeeId != me.Id && (
                    f.Employee.ManagerId == me.Id ||
                    (f.Employee.ManagerEmpCode != null && f.Employee.ManagerEmpCode.Trim() == myCode) ||
                    (f.Employee.ManagerNameCached != null && f.Employee.ManagerNameCached.Trim() == myName)
              ));

            // 2) GM department scope (if any)
            var gmDeptNames = await _db.ManagerScopes
                .Include(s => s.Departments)
                .Where(s => s.ManagerEmployeeId == me.Id && s.ScopeType == "GeneralManager")
                .SelectMany(s => s.Departments.Select(d => d.Department))
                .ToListAsync();

            IQueryable<Form> gmQ = Enumerable.Empty<Form>().AsQueryable();
            if (gmDeptNames.Count > 0)
            {
                gmQ = _db.Forms
                    .Include(f => f.Employee).Include(f => f.Cycle)
                    .Where(f => gmDeptNames.Contains(f.Employee.Department!));
            }

            // Merge & dedupe
            var allQ = teamQ;
            if (gmDeptNames.Count > 0) allQ = allQ.Union(gmQ);

            Drafts = await allQ.Where(f => f.Status == "Draft").OrderBy(f => f.Employee.Name).ToListAsync();
            ToReview = await allQ.Where(f => f.Status == "Submitted").OrderBy(f => f.Employee.Name).ToListAsync();
            InReview = await allQ.Where(f => f.Status == "MgrReviewed").OrderBy(f => f.Employee.Name).ToListAsync();
        }
    }
}
