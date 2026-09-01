using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsPortalBe.Migrations
{
    /// <inheritdoc />
    public partial class CourseEnrollments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lmsUser_lmsCourse_CourseModelId",
                table: "lmsUser");

            migrationBuilder.DropIndex(
                name: "IX_lmsUser_CourseModelId",
                table: "lmsUser");

            migrationBuilder.DropIndex(
                name: "IX_lmsCourse_Id",
                table: "lmsCourse");

            migrationBuilder.DropColumn(
                name: "CourseModelId",
                table: "lmsUser");

            migrationBuilder.CreateTable(
                name: "lmsCourseEnrollment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CourseId = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lmsCourseEnrollment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lmsCourseEnrollment_lmsCourse_CourseId",
                        column: x => x.CourseId,
                        principalTable: "lmsCourse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lmsCourseEnrollment_lmsUser_UserId",
                        column: x => x.UserId,
                        principalTable: "lmsUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lmsCourseEnrollment_CourseId",
                table: "lmsCourseEnrollment",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_lmsCourseEnrollment_UserId_CourseId",
                table: "lmsCourseEnrollment",
                columns: new[] { "UserId", "CourseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lmsCourseEnrollment");

            migrationBuilder.AddColumn<int>(
                name: "CourseModelId",
                table: "lmsUser",
                type: "INTEGER",
                nullable: true);

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
    }
}
