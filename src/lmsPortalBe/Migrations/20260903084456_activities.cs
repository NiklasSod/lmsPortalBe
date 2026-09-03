using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsPortalBe.Migrations
{
    /// <inheritdoc />
    public partial class activities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lmsActivity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lmsActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lmsActivity_lmsCourseModule_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "lmsCourseModule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lmsActivity_ModuleId",
                table: "lmsActivity",
                column: "ModuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lmsActivity");
        }
    }
}
