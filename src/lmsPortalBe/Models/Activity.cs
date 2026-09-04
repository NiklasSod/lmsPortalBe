namespace lmsPortalBe.Models
{
    public class Activity
    {
        public int Id { get; init; } = 0;
        public int ModuleId { get; set; }
        public CourseModule Module { get; set; } = null!;
        public ActivityType ActivityType { get; set; } = ActivityType.Lecture;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.UtcNow;
    }
}