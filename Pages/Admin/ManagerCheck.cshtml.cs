using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EmployeeModel = AppraisalPortal.Models.Employee;

namespace AppraisalPortal.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ManagerCheckModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public ManagerCheckModel(ApplicationDbContext db) { _db = db; }

        public string? QueryEmpCode { get; set; }
        public string? Error { get; set; }

        public EmployeeModel? Target { get; set; }
        public bool IsReportingManager { get; set; }
        public bool IsReportingManagerByScope { get; set; }
        public bool IsReportingManagerByTeam { get; set; }
        public List<string> GmDepartments { get; set; } = new();

        public List<(string EmpCode, string Name, string? Department)> DirectsStrong { get; set; } = new();
        public List<(string EmpCode, string Name, string? Department, string LinkType)> DirectsWeak { get; set; } = new();

        public async Task OnGetAsync(string? empCode)
        {
            QueryEmpCode = empCode?.Trim();
            if (string.IsNullOrWhiteSpace(QueryEmpCode)) return;

            Target = await _db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmpCode == QueryEmpCode);

            if (Target == null)
            {
                Error = $"Employee with EmpCode '{QueryEmpCode}' not found.";
                return;
            }

            // Strong directs (normalized FK)
            var strong = await _db.Employees.AsNoTracking()
                .Where(e => e.ManagerId == Target.Id)
                .OrderBy(e => e.Name)
                .ToListAsync();

            DirectsStrong = strong
                .Select(e => (e.EmpCode, e.Name, e.Department))
                .ToList();

            // Weak directs by EmpCode / Name cache
            var myCode = (Target.EmpCode ?? "").Trim();
            var myName = (Target.Name ?? "").Trim();

            var weakByCode = await _db.Employees.AsNoTracking()
                .Where(e => e.Id != Target.Id && e.ManagerEmpCode != null && e.ManagerEmpCode.Trim() == myCode)
                .OrderBy(e => e.Name)
                .ToListAsync();

            var weakByName = await _db.Employees.AsNoTracking()
                .Where(e => e.Id != Target.Id && e.ManagerNameCached != null && e.ManagerNameCached.Trim() == myName)
                .OrderBy(e => e.Name)
                .ToListAsync();

            // Merge weak lists with link-type
            var weakMerged = new Dictionary<int, (string EmpCode, string Name, string? Department, string LinkType)>();
            foreach (var e in weakByCode)
                weakMerged[e.Id] = (e.EmpCode, e.Name, e.Department, "By EmpCode");

            foreach (var e in weakByName)
                weakMerged[e.Id] = (e.EmpCode, e.Name, e.Department, weakMerged.ContainsKey(e.Id) ? "By Code + Name" : "By Name");

            DirectsWeak = weakMerged.Values
                .OrderBy(v => v.Name)
                .ToList();

            // GM departments (scope)
            var gmDepts = await _db.ManagerScopes
                .Include(s => s.Departments)
                .Where(s => s.ManagerEmployeeId == Target.Id && s.ScopeType == "GeneralManager")
                .SelectMany(s => s.Departments.Select(d => d.Department))
                .ToListAsync();

            GmDepartments = gmDepts;

            // Determine reporting manager flag
            IsReportingManagerByTeam = DirectsStrong.Any() || DirectsWeak.Any();

            IsReportingManagerByScope = await _db.ManagerScopes
                .AnyAsync(s => s.ManagerEmployeeId == Target.Id && s.ScopeType == "ReportingManager");

            IsReportingManager = IsReportingManagerByTeam || IsReportingManagerByScope;
        }
    }
}
