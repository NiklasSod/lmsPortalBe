namespace lmsPortalBe.Models
{
    public class Submission
    {
        public int Id { get; init; } = 0;
        public int AssignmentId { get; set; } = 0;
        public Assignment Assignment { get; set; } = null!;
        public string StudentId { get; set; } = string.Empty;
        public ApplicationUser Student { get; set; } = null!;
        public string Content { get; set; } = string.Empty;
        public string Feedback { get; set; } = string.Empty;
        public DateTime HandinDate { get; set; } = DateTime.UtcNow;
        public AssignmentStatus Status { get; set; } = AssignmentStatus.Unsent;
    }
}