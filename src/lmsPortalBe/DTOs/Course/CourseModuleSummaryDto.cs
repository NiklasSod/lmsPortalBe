using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Course;

public class CourseModuleSummaryDto
{
  public int Id { get; set; }

  [Required]
  public string Name { get; set; } = string.Empty;

  [Required]
  public string Description { get; set; } = string.Empty;

  [Required]
  public DateTime StartDate { get; set; } = DateTime.Now;

  [Required]
  public DateTime EndDate { get; set; } = DateTime.Now;


}