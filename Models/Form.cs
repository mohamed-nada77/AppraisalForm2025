// /Models/Form.cs

namespace AppraisalPortal.Models
{
    public class Form
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public Employee Employee { get; set; } = default!;

        public int CycleId { get; set; }
        public AppraisalCycle Cycle { get; set; } = default!;

        // Draft, Submitted, MgrReviewed, HRReviewed, Finalized (use as needed)
        public string Status { get; set; } = "Draft";

        // Legacy numeric rollups (optional)
        public decimal? EmployeeScore { get; set; }
        public decimal? ManagerScore { get; set; }
        public decimal? FinalScore { get; set; }

        // Comments used by Summary / PDF
        public string? SelfComments { get; set; }
        public string? ManagerComments { get; set; }
        public string? HRComments { get; set; }
        public string? CEOComments { get; set; }

        // Legacy Q&A responses (still supported)
        public ICollection<Response> Responses { get; set; } = new List<Response>();
    }
}
