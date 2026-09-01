using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Course;

public class CreateCourseModuleRequestDto
{
  [Required]
  public int CourseId { get; set; }
  
  [Required]
  public string Name { get; set; } = string.Empty;

  [Required]
  public string Description { get; set; } = string.Empty;

  [Required]
  public DateTime StartDate { get; set; }

  [Required]
  public DateTime EndDate { get; set; }
}
