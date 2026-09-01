using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsPortalBe.Migrations
{
    /// <inheritdoc />
    public partial class modules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lmsCourseModule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CourseId = table.Column<int>(type: "INTEGER", nullable: false),
                    CourseModelId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lmsCourseModule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lmsCourseModule_lmsCourse_CourseId",
                        column: x => x.CourseId,
                        principalTable: "lmsCourse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lmsCourseModule_lmsCourse_CourseModelId",
                        column: x => x.CourseModelId,
                        principalTable: "lmsCourse",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_lmsCourseModule_CourseId",
                table: "lmsCourseModule",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_lmsCourseModule_CourseModelId",
                table: "lmsCourseModule",
                column: "CourseModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lmsCourseModule");
        }
    }
}
