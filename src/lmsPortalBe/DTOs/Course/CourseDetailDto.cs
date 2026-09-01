namespace lmsPortalBe.DTOs.Course;

public class CourseDetailDto
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public DateTime StartDate { get; set; }
  public DateTime EndDate { get; set; }
  public List<CourseEnrollmentDto> Enrollments { get; set; } = [];
}
