// /Models/Response.cs

namespace AppraisalPortal.Models
{
    public class Response
    {
        public int Id { get; set; }

        public int FormId { get; set; }
        public Form Form { get; set; } = default!;

        public int QuestionId { get; set; }
        public Question Question { get; set; } = default!;

        public int? SelfRating { get; set; }
        public string? SelfComment { get; set; }
        public int? ManagerRating { get; set; }
        public string? ManagerComment { get; set; }
    }
}
