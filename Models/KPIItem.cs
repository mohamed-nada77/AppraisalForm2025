// /Models/KPIItem.cs

namespace AppraisalPortal.Models
{
    public class KPIItem
    {
        public int Id { get; set; }

        public int FormId { get; set; }
        public Form Form { get; set; } = default!;

        public string Description { get; set; } = string.Empty;
        public string? ActualPerformance { get; set; }
        public int Score { get; set; } // 0-100
    }
}