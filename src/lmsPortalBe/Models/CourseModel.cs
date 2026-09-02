namespace lmsPortalBe.Models
{
    public class CourseModel
    {
        public int Id { get; init; } = 0;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.UtcNow;
        public ICollection<CourseEnrollment> Enrollments { get; set; } = [];
        public ICollection<CourseModule> Modules { get; set; } = [];
    }
}