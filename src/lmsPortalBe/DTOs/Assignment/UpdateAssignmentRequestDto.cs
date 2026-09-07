namespace lmsPortalBe.DTOs.Course;

public class UpdateAssignmentRequestDto
{
  public int? ModuleId { get; set; }
  public string? Name { get; set; }
  public string? Description { get; set; }
  public DateTime? DueDate { get; set; }
}
