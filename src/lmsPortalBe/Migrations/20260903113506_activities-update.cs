using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsPortalBe.Migrations
{
    /// <inheritdoc />
    public partial class activitiesupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ActivityTypeId",
                table: "lmsActivity",
                newName: "ActivityType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ActivityType",
                table: "lmsActivity",
                newName: "ActivityTypeId");
        }
    }
}
