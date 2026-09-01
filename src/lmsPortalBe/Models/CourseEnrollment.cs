namespace lmsPortalBe.Models
{
  public class CourseEnrollment
  {
    public int Id { get; init; } = 0;
    public string UserId { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public CourseRole Role { get; set; } = CourseRole.Student;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public CourseModel Course { get; set; } = null!;
  }
}
