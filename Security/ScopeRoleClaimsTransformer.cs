// /Security/ScopeRoleClaimsTransformer.cs  (replace the class with this)
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AppraisalPortal.Models;

namespace AppraisalPortal.Security
{
    /// Adds role claims dynamically:
    /// - "Manager" if user has scope or directs (fixes Manager Inbox access)
    /// - "HR" if EmpCode == "88"
    /// - "CEO" if EmpCode == "7"
    public class ScopeRoleClaimsTransformer : IClaimsTransformation
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _db;

        public ScopeRoleClaimsTransformer(UserManager<AppUser> userManager, ApplicationDbContext db)
        { _userManager = userManager; _db = db; }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (!(principal.Identity?.IsAuthenticated ?? false)) return principal;

            // If they already have Admin, keep as-is (Admin sees all)
            if (principal.IsInRole("Admin")) return principal;

            var user = await _userManager.GetUserAsync(principal);
            if (user == null) return principal;

            var me = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (me == null) return principal;

            var baseId = principal.Identity as ClaimsIdentity ?? new ClaimsIdentity();
            var augmented = new ClaimsIdentity(
                baseId.Claims,
                baseId.AuthenticationType,
                baseId.NameClaimType,
                baseId.RoleClaimType);
            string roleType = augmented.RoleClaimType ?? ClaimTypes.Role;

            // === SAFETY NET ROLES BY EMPCODE ===
            var code = (me.EmpCode ?? "").Trim();
            if (code == "88" && !principal.IsInRole("HR"))
                augmented.AddClaim(new Claim(roleType, "HR"));
            if (code == "7" && !principal.IsInRole("CEO"))
                augmented.AddClaim(new Claim(roleType, "CEO"));

            // === MANAGER HEURISTIC ===
            if (!principal.IsInRole("Manager"))
            {
                var myCode = code;
                var myName = (me.Name ?? "").Trim();

                bool hasScope = await _db.ManagerScopes.AsNoTracking().AnyAsync(s => s.ManagerEmployeeId == me.Id);
                bool hasStrongDirects = await _db.Employees.AsNoTracking().AnyAsync(e => e.ManagerId == me.Id);
                bool hasWeakDirects = await _db.Employees.AsNoTracking().AnyAsync(e =>
                    e.Id != me.Id &&
                    ((e.ManagerEmpCode != null && e.ManagerEmpCode.Trim() == myCode) ||
                     (e.ManagerNameCached != null && e.ManagerNameCached.Trim() == myName)));

                if (hasScope || hasStrongDirects || hasWeakDirects)
                    augmented.AddClaim(new Claim(roleType, "Manager"));
            }

            return new ClaimsPrincipal(augmented);
        }
    }
}
