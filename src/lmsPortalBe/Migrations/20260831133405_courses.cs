using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsPortalBe.Migrations
{
    /// <inheritdoc />
    public partial class Courses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourseModelId",
                table: "lmsUser",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "lmsCourse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lmsCourse", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lmsUser_CourseModelId",
                table: "lmsUser",
                column: "CourseModelId");

            migrationBuilder.CreateIndex(
                name: "IX_lmsCourse_Id",
                table: "lmsCourse",
                column: "Id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_lmsUser_lmsCourse_CourseModelId",
                table: "lmsUser",
                column: "CourseModelId",
                principalTable: "lmsCourse",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lmsUser_lmsCourse_CourseModelId",
                table: "lmsUser");

            migrationBuilder.DropTable(
                name: "lmsCourse");

            migrationBuilder.DropIndex(
                name: "IX_lmsUser_CourseModelId",
                table: "lmsUser");

            migrationBuilder.DropColumn(
                name: "CourseModelId",
                table: "lmsUser");
        }
    }
}
