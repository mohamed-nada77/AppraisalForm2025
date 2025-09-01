// /Data/SeedData.cs
using AppraisalPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AppraisalPortal
{
    public static class SeedData
    {
        public static async Task RunAsync(IServiceProvider services)
        {
            var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = services.GetRequiredService<UserManager<AppUser>>();
            var db = services.GetRequiredService<ApplicationDbContext>();

            // 1) Ensure roles
            string[] roles = { "Admin", "Employee", "Manager", "HR", "CEO" };
            foreach (var r in roles)
                if (!await roleMgr.RoleExistsAsync(r))
                    await roleMgr.CreateAsync(new IdentityRole(r));

            // 2) Ensure fallback ADMIN always exists
            const string adminUser = "ADMIN";
            const string adminPass = "Admin#12345";

            var admin = await userMgr.FindByNameAsync(adminUser);
            if (admin == null)
            {
                admin = new AppUser
                {
                    UserName = adminUser,
                    Email = "admin@local",
                    EmailConfirmed = true,
                    DisplayName = "Administrator",
                    // not enforcing MustChangePassword here; change it manually if you want
                };
                var created = await userMgr.CreateAsync(admin, adminPass);
                if (!created.Succeeded)
                    throw new Exception($"Admin create failed: {string.Join(", ", created.Errors.Select(e => e.Description))}");
            }
            if (!await userMgr.IsInRoleAsync(admin, "Admin"))
                await userMgr.AddToRoleAsync(admin, "Admin");
            if (!await userMgr.IsInRoleAsync(admin, "Employee"))
                await userMgr.AddToRoleAsync(admin, "Employee");

            // 3) Promote 90902 (if present) to Admin + Manager (but do NOT return early)
            var u90902 = await userMgr.FindByNameAsync("90902");
            if (u90902 != null)
            {
                if (!await userMgr.IsInRoleAsync(u90902, "Admin"))
                    await userMgr.AddToRoleAsync(u90902, "Admin");
                if (!await userMgr.IsInRoleAsync(u90902, "Manager"))
                    await userMgr.AddToRoleAsync(u90902, "Manager");
                if (!await userMgr.IsInRoleAsync(u90902, "Employee"))
                    await userMgr.AddToRoleAsync(u90902, "Employee");
            }

            // 4) HR Manager = EmpCode 88; CEO = EmpCode 7 (if users exist)
            // /Data/SeedData.cs  (snippet)
            async Task EnsureRoleByUserName(string userName, string role)
            {
                var u = await userMgr.FindByNameAsync(userName);
                if (u != null && !await userMgr.IsInRoleAsync(u, role))
                    await userMgr.AddToRoleAsync(u, role);
            }
            // HR Manager = EmpCode 88; CEO = EmpCode 7
            await EnsureRoleByUserName("88", "HR");
            await EnsureRoleByUserName("7", "CEO");


            // 5) Make sure every existing Identity user at least has Employee role
            foreach (var u in userMgr.Users)
            {
                if (!await userMgr.IsInRoleAsync(u, "Employee"))
                    await userMgr.AddToRoleAsync(u, "Employee");
            }

            // 6) Ensure Manager scopes from Employees with Manager responsibilities (optional – keep your existing logic)
            // If you rely on ManagerScopes + dynamic Manager role, keep your earlier ManagerScope creation code here.

            await db.SaveChangesAsync();
        }
    }
}
