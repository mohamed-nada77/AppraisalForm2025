using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AppraisalPortal.Models;

namespace AppraisalPortal.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ImportModel : PageModel
    {
        private readonly EmployeeImportService _svc;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _um;

        public ImportModel(EmployeeImportService svc, ApplicationDbContext db, UserManager<AppUser> um)
        {
            _svc = svc; _db = db; _um = um;
        }

        [BindProperty] public IFormFile? Upload { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Upload == null)
            {
                TempData["Err"] = "Please choose a file.";
                return RedirectToPage();
            }

            try
            {
                using var s = Upload.OpenReadStream();
                var summary = await _svc.ImportAsync(s);

                if (summary.CreatedCredentials.Any())
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.AddWorksheet("New Credentials");
                    ws.Cell(1, 1).Value = "EmpCode";
                    ws.Cell(1, 2).Value = "Password";
                    int r = 2;
                    foreach (var (code, pass) in summary.CreatedCredentials)
                    {
                        ws.Cell(r, 1).Value = code;
                        ws.Cell(r, 2).Value = pass;
                        r++;
                    }

                    using var ms = new MemoryStream();
                    wb.SaveAs(ms);
                    ms.Position = 0;
                    return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "NewUserCredentials.xlsx");
                }

                TempData["Msg"] = "Import completed (no new users).";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["Err"] = $"Import failed: {ex.Message}";
                return RedirectToPage();
            }
        }

        // Wipe EVERYTHING
        public async Task<IActionResult> OnPostWipeAsync(string? confirmText, bool? alsoDeleteUsers, bool? alsoRemoveQuestions)
        {
            if (!string.Equals(confirmText?.Trim(), "WIPE ALL", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrWipe"] = "Confirmation text mismatch. Type exactly: WIPE ALL";
                return RedirectToPage();
            }

            try
            {
                _db.Responsibilities.RemoveRange(_db.Responsibilities);
                _db.KPIItems.RemoveRange(_db.KPIItems);
                _db.SoftSkillRatings.RemoveRange(_db.SoftSkillRatings);
                _db.Responses.RemoveRange(_db.Responses);

                var approvalEntity = _db.Model.FindEntityType(typeof(Approval));
                if (approvalEntity != null)
                    _db.RemoveRange(_db.Set<Approval>());

                _db.Forms.RemoveRange(_db.Forms);
                _db.AppraisalCycles.RemoveRange(_db.AppraisalCycles);
                _db.ManagerScopeDepartments.RemoveRange(_db.ManagerScopeDepartments);
                _db.ManagerScopes.RemoveRange(_db.ManagerScopes);

                if (alsoRemoveQuestions == true)
                    _db.Questions.RemoveRange(_db.Questions);

                foreach (var e in _db.Employees) e.ManagerId = null;
                await _db.SaveChangesAsync();

                _db.Employees.RemoveRange(_db.Employees);
                await _db.SaveChangesAsync();

                if (alsoDeleteUsers == true)
                {
                    var allUsers = _um.Users.ToList();
                    foreach (var u in allUsers)
                    {
                        if (string.Equals(u.UserName, "ADMIN", StringComparison.OrdinalIgnoreCase)) continue;
                        if (string.Equals(u.UserName, "90902", StringComparison.OrdinalIgnoreCase)) continue;
                        await _um.DeleteAsync(u);
                    }
                }

                TempData["MsgWipe"] = "All appraisal data wiped successfully.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrWipe"] = $"Wipe failed: {ex.Message}";
                return RedirectToPage();
            }
        }

        // Delete ONLY Employees (+ dependents) — keep cycles & questions
        public async Task<IActionResult> OnPostWipeEmployeesAsync(string? confirmText, bool? alsoDeleteUsers)
        {
            if (!string.Equals(confirmText?.Trim(), "WIPE EMPLOYEES", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrWipeEmp"] = "Confirmation text mismatch. Type exactly: WIPE EMPLOYEES";
                return RedirectToPage();
            }

            try
            {
                // Remove employee-owned data first
                _db.Responsibilities.RemoveRange(_db.Responsibilities);
                _db.KPIItems.RemoveRange(_db.KPIItems);
                _db.SoftSkillRatings.RemoveRange(_db.SoftSkillRatings);
                _db.Responses.RemoveRange(_db.Responses);
                _db.Forms.RemoveRange(_db.Forms);
                _db.ManagerScopeDepartments.RemoveRange(_db.ManagerScopeDepartments);
                _db.ManagerScopes.RemoveRange(_db.ManagerScopes);

                foreach (var e in _db.Employees) e.ManagerId = null;
                await _db.SaveChangesAsync();

                _db.Employees.RemoveRange(_db.Employees);
                await _db.SaveChangesAsync();

                if (alsoDeleteUsers == true)
                {
                    var allUsers = _um.Users.ToList();
                    foreach (var u in allUsers)
                    {
                        if (string.Equals(u.UserName, "ADMIN", StringComparison.OrdinalIgnoreCase)) continue;
                        if (string.Equals(u.UserName, "90902", StringComparison.OrdinalIgnoreCase)) continue;
                        await _um.DeleteAsync(u);
                    }
                }

                TempData["MsgWipeEmp"] = "All employees and dependent appraisal data deleted.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrWipeEmp"] = $"Delete employees failed: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}
