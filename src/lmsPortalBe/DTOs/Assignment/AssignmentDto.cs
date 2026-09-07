using System.ComponentModel.DataAnnotations;

namespace lmsPortalBe.DTOs.Course;

public class AssignmentDto
{
  public int Id { get; set; }
  [Required]
  public int ModuleId { get; set; }

  [Required]
  public string Status { get; set; } = string.Empty;

  [Required]
  public string Name { get; set; } = string.Empty;

  [Required]
  public string Description { get; set; } = string.Empty;

  [Required]
  public DateTime DueDate { get; set; } = DateTime.UtcNow;


}