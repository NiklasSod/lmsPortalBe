using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsPortalBe.Migrations
{
    /// <inheritdoc />
    public partial class modules2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lmsCourseModule_lmsCourse_CourseModelId",
                table: "lmsCourseModule");

            migrationBuilder.DropIndex(
                name: "IX_lmsCourseModule_CourseModelId",
                table: "lmsCourseModule");

            migrationBuilder.DropColumn(
                name: "CourseModelId",
                table: "lmsCourseModule");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourseModelId",
                table: "lmsCourseModule",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_lmsCourseModule_CourseModelId",
                table: "lmsCourseModule",
                column: "CourseModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_lmsCourseModule_lmsCourse_CourseModelId",
                table: "lmsCourseModule",
                column: "CourseModelId",
                principalTable: "lmsCourse",
                principalColumn: "Id");
        }
    }
}
