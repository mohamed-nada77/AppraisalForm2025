// /Models/Employee.cs

namespace AppraisalPortal.Models
{
    public class Employee
    {
        public int Id { get; set; }

        public string EmpCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public string? Email { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public string? Location { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public DateTime? DateOfBirth { get; set; }

        // Primary normalized link (preferred)
        public int? ManagerId { get; set; }
        public Employee? Manager { get; set; }

        // Fallbacks (populated from import) – used when ManagerId wasn't linked
        public string? ManagerEmpCode { get; set; }      // e.g., ReportingManagerCode
        public string? ManagerNameCached { get; set; }   // e.g., ReportingManagerName

        public string? UserId { get; set; }
        public AppUser? User { get; set; }
    }
}