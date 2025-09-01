// /Models/Approvals.cs

namespace AppraisalPortal.Models
{
    public class Approval
    {
        public int Id { get; set; }

        public int FormId { get; set; }
        public Form Form { get; set; } = default!;

        public string Step { get; set; } = "Employee"; // Employee, Manager, HR, CEO
        public string ApproverUserId { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending/Approved/Rejected
        public string? Comment { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
