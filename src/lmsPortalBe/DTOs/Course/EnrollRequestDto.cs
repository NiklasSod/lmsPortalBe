using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Course;

public class EnrollRequestDto
{
  [Required]
  public int CourseId { get; set; }
}
