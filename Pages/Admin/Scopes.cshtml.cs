// /Pages/Admin/Scopes.cshtml.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AppraisalPortal.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ScopesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _um;
        private readonly RoleManager<IdentityRole> _rm;

        public ScopesModel(ApplicationDbContext db, UserManager<AppUser> um, RoleManager<IdentityRole> rm)
        { _db = db; _um = um; _rm = rm; }

        public IList<ManagerScope> Scopes { get; set; } = new List<ManagerScope>();
        public string[] KnownDepartments { get; } =
        {
            "2D Department", "3D Department", "FF&E", "CIVIL", "MEP",
            "PLANNING", "TECHNICAL DEPARTMENT", "Procurement", "BUSINESS DEVELOPMENT"
        };

        public async Task OnGetAsync()
        {
            Scopes = await _db.ManagerScopes
                .Include(s => s.ManagerEmployee)
                .Include(s => s.Departments)
                .OrderBy(s => s.ManagerEmployee.EmpCode).ToListAsync();
        }

        // Add or update single scope
        public async Task<IActionResult> OnPostUpsertAsync(string EmpCode, string ScopeType, string? Notes, string[]? Departments)
        {
            if (string.IsNullOrWhiteSpace(EmpCode))
            {
                TempData["Err"] = "Emp Code is required.";
                return RedirectToPage();
            }

            var emp = await _db.Employees.FirstOrDefaultAsync(e => e.EmpCode == EmpCode.Trim());
            if (emp == null)
            {
                TempData["Err"] = $"Employee with EmpCode {EmpCode} not found.";
                return RedirectToPage();
            }

            // ensure Manager Identity role when scope implies managerial responsibility
            var user = await _um.FindByIdAsync(emp.UserId!);
            if (user != null)
            {
                if (!await _rm.RoleExistsAsync("Manager")) await _rm.CreateAsync(new IdentityRole("Manager"));
                if (!await _um.IsInRoleAsync(user, "Manager")) await _um.AddToRoleAsync(user, "Manager");
            }

            var scope = await _db.ManagerScopes
                .Include(s => s.Departments)
                .FirstOrDefaultAsync(s => s.ManagerEmployeeId == emp.Id);

            if (scope == null)
            {
                scope = new ManagerScope { ManagerEmployeeId = emp.Id, ScopeType = ScopeType, Notes = Notes };
                _db.ManagerScopes.Add(scope);
            }
            else
            {
                scope.ScopeType = ScopeType;
                scope.Notes = Notes;
                _db.ManagerScopes.Update(scope);
                // clear departments to re-add
                _db.ManagerScopeDepartments.RemoveRange(scope.Departments);
                scope.Departments.Clear();
            }

            if (ScopeType == "GeneralManager" && Departments != null && Departments.Length > 0)
            {
                foreach (var d in Departments.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    scope.Departments.Add(new ManagerScopeDepartment { Department = d });
                }
            }

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Scope saved.";
            return RedirectToPage();
        }

        // Bulk: mark codes as ReportingManager + ensure role
        public async Task<IActionResult> OnPostBulkAsync(string? Codes)
        {
            if (string.IsNullOrWhiteSpace(Codes))
            {
                TempData["Err"] = "No codes provided.";
                return RedirectToPage();
            }

            var tokens = Codes
                .Replace("\r", " ").Replace("\n", " ").Replace("\t", " ")
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Distinct().ToList();

            int created = 0; int ensuredRole = 0;
            if (!await _rm.RoleExistsAsync("Manager")) await _rm.CreateAsync(new IdentityRole("Manager"));

            foreach (var code in tokens)
            {
                var emp = await _db.Employees.FirstOrDefaultAsync(e => e.EmpCode == code);
                if (emp == null) continue;

                var user = await _um.FindByIdAsync(emp.UserId!);
                if (user != null && !await _um.IsInRoleAsync(user, "Manager"))
                {
                    await _um.AddToRoleAsync(user, "Manager");
                    ensuredRole++;
                }

                var exists = await _db.ManagerScopes.AnyAsync(s => s.ManagerEmployeeId == emp.Id);
                if (!exists)
                {
                    _db.ManagerScopes.Add(new ManagerScope { ManagerEmployeeId = emp.Id, ScopeType = "ReportingManager" });
                    created++;
                }
            }

            await _db.SaveChangesAsync();
            TempData["Msg"] = $"Bulk complete. Scopes created: {created}. Role ensured for: {ensuredRole}.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var scope = await _db.ManagerScopes.Include(s => s.Departments).FirstOrDefaultAsync(s => s.Id == id);
            if (scope != null)
            {
                _db.ManagerScopeDepartments.RemoveRange(scope.Departments);
                _db.ManagerScopes.Remove(scope);
                await _db.SaveChangesAsync();
                TempData["Msg"] = "Scope deleted.";
            }
            return RedirectToPage();
        }
    }
}
