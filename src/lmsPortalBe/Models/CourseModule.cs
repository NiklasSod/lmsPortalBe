namespace lmsPortalBe.Models
{
    public class CourseModule
    {
        public int Id { get; init; } = 0;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now;
        public int CourseId { get; set; }
        public CourseModel Course { get; set; } = null!;
    }
}