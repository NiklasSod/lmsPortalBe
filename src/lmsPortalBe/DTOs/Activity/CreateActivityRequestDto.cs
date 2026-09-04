using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Course;

public class CreateActivityRequestDto
{
  [Required]
  public int ModuleId { get; set; }

  [Required]
  public string Type { get; set; } = string.Empty;
  
  [Required]
  public string Name { get; set; } = string.Empty;

  [Required]
  public string Description { get; set; } = string.Empty;

  [Required]
  public DateTime StartDate { get; set; }

  [Required]
  public DateTime EndDate { get; set; }
}
