// /Models/AppraisalCycle.cs

namespace AppraisalPortal.Models
{
    public class AppraisalCycle
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Status { get; set; } = "Open";
    }
}