using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsPortalBe.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lmsResource_lmsActivity_ActivityId",
                table: "lmsResource");

            migrationBuilder.DropForeignKey(
                name: "FK_lmsResource_lmsCourseModule_ModuleId",
                table: "lmsResource");

            migrationBuilder.DropForeignKey(
                name: "FK_lmsResource_lmsCourse_CourseId",
                table: "lmsResource");

            migrationBuilder.AddForeignKey(
                name: "FK_lmsResource_lmsActivity_ActivityId",
                table: "lmsResource",
                column: "ActivityId",
                principalTable: "lmsActivity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_lmsResource_lmsCourseModule_ModuleId",
                table: "lmsResource",
                column: "ModuleId",
                principalTable: "lmsCourseModule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_lmsResource_lmsCourse_CourseId",
                table: "lmsResource",
                column: "CourseId",
                principalTable: "lmsCourse",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lmsResource_lmsActivity_ActivityId",
                table: "lmsResource");

            migrationBuilder.DropForeignKey(
                name: "FK_lmsResource_lmsCourseModule_ModuleId",
                table: "lmsResource");

            migrationBuilder.DropForeignKey(
                name: "FK_lmsResource_lmsCourse_CourseId",
                table: "lmsResource");

            migrationBuilder.AddForeignKey(
                name: "FK_lmsResource_lmsActivity_ActivityId",
                table: "lmsResource",
                column: "ActivityId",
                principalTable: "lmsActivity",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_lmsResource_lmsCourseModule_ModuleId",
                table: "lmsResource",
                column: "ModuleId",
                principalTable: "lmsCourseModule",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_lmsResource_lmsCourse_CourseId",
                table: "lmsResource",
                column: "CourseId",
                principalTable: "lmsCourse",
                principalColumn: "Id");
        }
    }
}
