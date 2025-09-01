// /Models/Responsibility.cs

namespace AppraisalPortal.Models
{
    public class Responsibility
    {
        public int Id { get; set; }

        public int FormId { get; set; }
        public Form Form { get; set; } = default!;

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int AchievementPercent { get; set; } // 0-100
    }
}
