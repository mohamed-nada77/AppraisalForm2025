// /Models/ManagerScope.cs

namespace AppraisalPortal.Models
{
    // Admin-configurable scope for a manager or a general manager
    public class ManagerScope
    {
        public int Id { get; set; }

        // FK to Employees table
        public int ManagerEmployeeId { get; set; }
        public Employee ManagerEmployee { get; set; } = default!;

        // "ReportingManager" or "GeneralManager"
        public string ScopeType { get; set; } = "ReportingManager";

        // Optional notes
        public string? Notes { get; set; }

        // Departments used when ScopeType == GeneralManager
        public ICollection<ManagerScopeDepartment> Departments { get; set; } = new List<ManagerScopeDepartment>();
    }
}
