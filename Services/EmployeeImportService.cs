using System.Globalization;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AppraisalPortal.Models;

namespace AppraisalPortal
{
    public class EmployeeImportService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<AppUser> _um;
        private readonly ILogger<EmployeeImportService> _log;
        private readonly PasswordRuleService _pwd;

        public EmployeeImportService(ApplicationDbContext db, UserManager<AppUser> um, ILogger<EmployeeImportService> log, PasswordRuleService pwd)
        { _db = db; _um = um; _log = log; _pwd = pwd; }

        public class ImportSummary
        {
            public int UsersCreated { get; set; }
            public int EmployeesInserted { get; set; }
            public int EmployeesUpdated { get; set; }
            public int ManagerLinksSet { get; set; }
            public List<(string EmpCode, string Password)> CreatedCredentials { get; set; } = new();
        }

        private static readonly string[] DateFormats = new[]
        {
            "dd-MM-yyyy","d-M-yyyy","dd/MM/yyyy","d/M/yyyy",
            "yyyy-MM-dd","yyyy/M/d","MM/dd/yyyy","M/d/yyyy"
        };

        private static string S(IXLCell c) => (c.GetString() ?? string.Empty).Trim();

        private static DateTime? ReadDate(IXLCell cell)
        {
            // 1) Native DateTime cell
            if (cell.TryGetValue<DateTime>(out var d))
                return d.Date;

            // 2) OADate numeric cell (Excel serial)
            if (cell.DataType == XLDataType.Number)
            {
                var n = cell.GetDouble();
                // Excel OADate valid-ish range (avoid tiny/huge numbers)
                if (n > 59 && n < 60000)
                    return DateTime.FromOADate(n).Date;
            }

            // 3) Text with day-first preference
            var raw = (cell.GetString() ?? "").Trim();
            if (string.IsNullOrEmpty(raw)) return null;

            if (DateTime.TryParseExact(raw, DateFormats, new CultureInfo("en-GB"), DateTimeStyles.None, out var d2))
                return d2.Date;

            // 4) Best-effort general parse, en-GB prefers day-first
            if (DateTime.TryParse(raw, new CultureInfo("en-GB"), DateTimeStyles.AssumeLocal, out var d3))
                return d3.Date;

            return null;
        }

        public async Task<ImportSummary> ImportAsync(Stream excel, CancellationToken ct = default)
        {
            var summary = new ImportSummary();

            using var wb = new XLWorkbook(excel);
            var ws = wb.Worksheet(1);
            var used = ws.RangeUsed();
            if (used is null) return summary;

            var rows = used.RowsUsed().Skip(1).ToList();
            var byCode = await _db.Employees.AsNoTracking().ToDictionaryAsync(e => e.EmpCode, ct);

            // PASS 1: create/ensure users + upsert employees
            foreach (var r in rows)
            {
                ct.ThrowIfCancellationRequested();

                // Columns: 1 SNO, 2 EmpCode, 3 Name, 4 ManagerName, 5 ManagerCode,
                //          6 Location, 7 Dept, 8 Desig, 9 DoJ, 10 DoB, 11 Email
                string empCode = S(r.Cell(2)); if (string.IsNullOrWhiteSpace(empCode)) continue;
                string name = S(r.Cell(3));
                string mgrName = S(r.Cell(4));
                string mgrCode = S(r.Cell(5));
                string loc = S(r.Cell(6));
                string dept = S(r.Cell(7));
                string desig = S(r.Cell(8));
                DateTime? doj = ReadDate(r.Cell(9));
                DateTime? dob = ReadDate(r.Cell(10));
                string email = S(r.Cell(11));

                // Ensure Identity user
                var user = await _um.FindByNameAsync(empCode);
                if (user == null)
                {
                    // Generate password per rule: FirstInitial + ddMMyyyy (DOB -> DOJ -> 01011990)
                    var tmpEmpForPwd = new Models.Employee { EmpCode = empCode, Name = name, DateOfBirth = dob, DateOfJoining = doj };
                    var pwd = _pwd.Generate(tmpEmpForPwd);

                    user = new AppUser
                    {
                        UserName = empCode,
                        Email = string.IsNullOrWhiteSpace(email) ? null : email,
                        EmailConfirmed = true,
                        DisplayName = string.IsNullOrWhiteSpace(name) ? empCode : name
                    };

                    var create = await _um.CreateAsync(user, pwd);
                    if (!create.Succeeded)
                    {
                        _log.LogWarning("Create user {EmpCode} failed: {Errors}", empCode, string.Join("; ", create.Errors.Select(e => e.Description)));
                        continue;
                    }

                    summary.UsersCreated++;
                    summary.CreatedCredentials.Add((empCode, pwd));
                    await _um.AddToRoleAsync(user, "Employee");
                }

                // Upsert Employee row
                if (!byCode.TryGetValue(empCode, out var emp))
                {
                    emp = new Models.Employee
                    {
                        EmpCode = empCode,
                        Name = name,
                        Email = string.IsNullOrWhiteSpace(email) ? null : email,
                        Department = dept,
                        Designation = desig,
                        Location = loc,
                        DateOfJoining = doj,
                        DateOfBirth = dob,
                        UserId = user.Id,
                        ManagerEmpCode = string.IsNullOrWhiteSpace(mgrCode) ? null : mgrCode,
                        ManagerNameCached = string.IsNullOrWhiteSpace(mgrName) ? null : mgrName
                    };
                    _db.Employees.Add(emp);
                    byCode[empCode] = emp;
                    summary.EmployeesInserted++;
                }
                else
                {
                    bool ch = false;
                    if (emp.Name != name) { emp.Name = name; ch = true; }
                    var mail = string.IsNullOrWhiteSpace(email) ? null : email;
                    if (emp.Email != mail) { emp.Email = mail; ch = true; }
                    if (emp.Department != dept) { emp.Department = dept; ch = true; }
                    if (emp.Designation != desig) { emp.Designation = desig; ch = true; }
                    if (emp.Location != loc) { emp.Location = loc; ch = true; }
                    if (emp.DateOfJoining != doj) { emp.DateOfJoining = doj; ch = true; }
                    if (emp.DateOfBirth != dob) { emp.DateOfBirth = dob; ch = true; }
                    if (emp.UserId != user.Id) { emp.UserId = user.Id; ch = true; }
                    var mcode = string.IsNullOrWhiteSpace(mgrCode) ? null : mgrCode;
                    if (emp.ManagerEmpCode != mcode) { emp.ManagerEmpCode = mcode; ch = true; }
                    var mname = string.IsNullOrWhiteSpace(mgrName) ? null : mgrName;
                    if (emp.ManagerNameCached != mname) { emp.ManagerNameCached = mname; ch = true; }
                    if (ch) _db.Employees.Update(emp);
                    summary.EmployeesUpdated += ch ? 1 : 0;
                }
            }
            await _db.SaveChangesAsync(ct);

            // PASS 2: link managers by EmpCode
            foreach (var r in rows)
            {
                string empCode = S(r.Cell(2)); if (string.IsNullOrWhiteSpace(empCode)) continue;
                string mgrCode = S(r.Cell(5)); if (string.IsNullOrWhiteSpace(mgrCode)) continue;

                if (byCode.TryGetValue(empCode, out var emp) && byCode.TryGetValue(mgrCode, out var mgr))
                {
                    if (emp.ManagerId != mgr.Id)
                    {
                        emp.ManagerId = mgr.Id;
                        _db.Employees.Update(emp);
                        summary.ManagerLinksSet++;
                    }
                }
            }
            await _db.SaveChangesAsync(ct);

            // Promote 90902 if present
            var u90902 = await _um.FindByNameAsync("90902");
            if (u90902 != null)
            {
                if (!await _um.IsInRoleAsync(u90902, "Admin")) await _um.AddToRoleAsync(u90902, "Admin");
                if (!await _um.IsInRoleAsync(u90902, "Manager")) await _um.AddToRoleAsync(u90902, "Manager");
            }

            return summary;
        }
    }
}
