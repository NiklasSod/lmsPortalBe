using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsPortalBe.Migrations
{
    /// <inheritdoc />
    public partial class resources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lmsResource",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatorId = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    UploadDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CourseId = table.Column<int>(type: "INTEGER", nullable: true),
                    ActivityId = table.Column<int>(type: "INTEGER", nullable: true),
                    ModuleId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lmsResource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lmsResource_lmsActivity_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "lmsActivity",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_lmsResource_lmsCourseModule_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "lmsCourseModule",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_lmsResource_lmsCourse_CourseId",
                        column: x => x.CourseId,
                        principalTable: "lmsCourse",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_lmsResource_lmsUser_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "lmsUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lmsResource_ActivityId",
                table: "lmsResource",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_lmsResource_CourseId",
                table: "lmsResource",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_lmsResource_CreatorId",
                table: "lmsResource",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_lmsResource_ModuleId",
                table: "lmsResource",
                column: "ModuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lmsResource");
        }
    }
}
