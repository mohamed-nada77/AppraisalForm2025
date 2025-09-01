// /Models/Question.cs

namespace AppraisalPortal.Models
{
    public class Question
    {
        public int Id { get; set; }
        public string Section { get; set; } = "General";
        public string Text { get; set; } = string.Empty;
        public int MaxRating { get; set; } = 5;
        public decimal Weight { get; set; } = 1m;
    }
}
