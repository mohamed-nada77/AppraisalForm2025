// /Pages/Index.cshtml.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AppraisalPortal.Pages
{
    public class IndexModel : PageModel
    {
        private readonly SignInManager<AppUser> _sm;
        private readonly UserManager<AppUser> _um;
        private readonly ApplicationDbContext _db;

        public IndexModel(SignInManager<AppUser> sm, UserManager<AppUser> um, ApplicationDbContext db)
        {
            _sm = sm; _um = um; _db = db;
        }

        // ---- UI flags used by Index.cshtml
        public bool IsSignedIn { get; private set; }
        public bool IsEmployee { get; private set; }
        public bool IsManagerLike { get; private set; }
        public bool IsHR { get; private set; }
        public bool IsCEO { get; private set; }
        public bool IsAdmin { get; private set; }

        // ---- UI text used by Index.cshtml (re-added to fix CS1061)
        public string WelcomeName { get; private set; } = "";
        public string Department { get; private set; } = "";
        public string ManagerDisplay { get; private set; } = "";
        public string EmpCode { get; private set; } = "";

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public async Task OnGetAsync(string? returnUrl)
        {
            ReturnUrl = returnUrl;
            IsSignedIn = _sm.IsSignedIn(User);
            if (!IsSignedIn) return;

            var user = await _um.GetUserAsync(User);
            if (user == null)
            {
                // stale cookie or deleted user – show login form instead of querying with null
                IsSignedIn = false;
                return;
            }

            // roles
            IsEmployee = await _um.IsInRoleAsync(user, "Employee");
            IsHR = await _um.IsInRoleAsync(user, "HR");
            IsCEO = await _um.IsInRoleAsync(user, "CEO");
            IsAdmin = await _um.IsInRoleAsync(user, "Admin");

            // employee record (safe include)
            var emp = await _db.Employees.AsNoTracking()
                .Include(e => e.Manager)
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            // fill UI strings (all null-safe)
            WelcomeName = emp?.Name?.Trim() ?? user.DisplayName ?? user.UserName ?? "";
            Department = string.IsNullOrWhiteSpace(emp?.Department) ? "Unassigned" : emp!.Department!;
            ManagerDisplay = emp?.Manager?.Name
                             ?? emp?.ManagerNameCached
                             ?? emp?.ManagerEmpCode
                             ?? "—";
            EmpCode = emp?.EmpCode ?? user.UserName ?? "—";

            // “manager-like” heuristic if not Manager/Admin by role
            IsManagerLike = IsAdmin || await _um.IsInRoleAsync(user, "Manager");
            if (!IsManagerLike && emp != null)
            {
                var myCode = (emp.EmpCode ?? "").Trim();
                var myName = (emp.Name ?? "").Trim();

                var byScope = await _db.ManagerScopes.AsNoTracking()
                    .AnyAsync(s => s.ManagerEmployeeId == emp.Id);

                var byTeam = await _db.Employees.AsNoTracking()
                    .AnyAsync(e =>
                        e.Id != emp.Id &&
                        (e.ManagerId == emp.Id ||
                         (e.ManagerEmpCode != null && e.ManagerEmpCode.Trim() == myCode) ||
                         (e.ManagerNameCached != null && e.ManagerNameCached.Trim() == myName)));

                IsManagerLike = byScope || byTeam;
            }
        }

        // ---- login from the home page
        public class LoginVm
        {
            public string EmpCode { get; set; } = "";
            public string Password { get; set; } = "";
            public bool RememberMe { get; set; }
        }

        [BindProperty] public LoginVm Input { get; set; } = new();

        public async Task<IActionResult> OnPostLoginAsync()
        {
            var result = await _sm.PasswordSignInAsync(Input.EmpCode, Input.Password, Input.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var dest = string.IsNullOrWhiteSpace(ReturnUrl) ? Url.Content("~/")! : ReturnUrl!;
                return LocalRedirect(dest);
            }

            ModelState.AddModelError(string.Empty, "Invalid Emp Code or password.");
            IsSignedIn = false;
            return Page();
        }
    }
}
