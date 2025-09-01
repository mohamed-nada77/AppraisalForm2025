// /Models/ManagerScopeDepartment.cs

namespace AppraisalPortal.Models
{
    public class ManagerScopeDepartment
    {
        public int Id { get; set; }

        public int ManagerScopeId { get; set; }
        public ManagerScope ManagerScope { get; set; } = default!;

        public string Department { get; set; } = string.Empty; // e.g. "MEP"
    }
}
