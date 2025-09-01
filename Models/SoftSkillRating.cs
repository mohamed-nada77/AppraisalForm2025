// /Models/SoftSkillRating.cs

namespace AppraisalPortal.Models
{
    public class SoftSkillRating
    {
        public int Id { get; set; }

        public int FormId { get; set; }
        public Form Form { get; set; } = default!;

        public string Attribute { get; set; } = string.Empty;
        public int Score { get; set; } // 1-10
    }
}
