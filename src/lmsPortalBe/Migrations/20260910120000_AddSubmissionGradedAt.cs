using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsPortalBe.Migrations
{
  /// <inheritdoc />
  public partial class AddSubmissionGradedAt : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<DateTime>(
          name: "GradedAt",
          table: "lmsSubmission",
          type: "TEXT",
          nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
          name: "GradedAt",
          table: "lmsSubmission");
    }
  }
}
