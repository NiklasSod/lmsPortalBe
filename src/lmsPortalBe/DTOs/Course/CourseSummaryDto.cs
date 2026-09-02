using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Course;

public class CourseSummaryDto
{
  public int Id { get; set; }

  [Required]
  public string Name { get; set; } = string.Empty;

  [Required]
  public string Description { get; set; } = string.Empty;

  [Required]
  public DateTime StartDate { get; set; } = DateTime.UtcNow;

  [Required]
  public DateTime EndDate { get; set; } = DateTime.UtcNow;


}