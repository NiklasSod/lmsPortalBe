namespace lmsPortalBe.DTOs.Course;

public class UpdateActivityRequestDto
{
  public int? ModuleId { get; set; }
  public string? Type { get; set; } = string.Empty;
  public string? Name { get; set; }
  public string? Description { get; set; }
  public DateTime? StartDate { get; set; }
  public DateTime? EndDate { get; set; }
}
