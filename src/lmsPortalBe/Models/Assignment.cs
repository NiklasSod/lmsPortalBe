namespace lmsPortalBe.Models
{
    public class Assignment
    {
        public int Id { get; init; } = 0;
        public int ModuleId { get; set; }
        public CourseModule Module { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; } = DateTime.UtcNow;
        public AssignmentStatus Status { get; set; }  = AssignmentStatus.Unsent;
    }
}