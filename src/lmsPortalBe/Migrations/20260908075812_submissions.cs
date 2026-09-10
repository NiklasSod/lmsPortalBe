using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsPortalBe.Migrations
{
    /// <inheritdoc />
    public partial class submissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lmsSubmission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AssignmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    StudentId = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Feedback = table.Column<string>(type: "TEXT", nullable: false),
                    HandinDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lmsSubmission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lmsSubmission_lmsAssignment_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "lmsAssignment",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_lmsSubmission_lmsUser_StudentId",
                        column: x => x.StudentId,
                        principalTable: "lmsUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lmsSubmission_AssignmentId",
                table: "lmsSubmission",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_lmsSubmission_StudentId_AssignmentId",
                table: "lmsSubmission",
                columns: new[] { "StudentId", "AssignmentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lmsSubmission");
        }
    }
}
