namespace lmsPortalBe.Models
{
    public class Submission
    {
        public int Id { get; init; } = 0;
        public int AssignmentId { get; set; } = 0;
        // TODO add this with SubMissions 
        // public Assignment Assignment { get; set; } = null!;
        // TODO - check - Student id is String (?)
        public int StudentId { get; set; } = 0;
        public string Content { get; set; } = string.Empty;
        public string Feedback { get; set; } = string.Empty;
        public DateTime HandinDate { get; set; } = DateTime.UtcNow;
        public AssignmentStatus Status { get; set; } = AssignmentStatus.Unsent;
    }
}