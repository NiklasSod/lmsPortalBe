using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsPortalBe.Migrations
{
    /// <inheritdoc />
    public partial class RenameAgeToDateOfBirth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Age",
                table: "lmsUserProfile");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "lmsUserProfile",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "lmsUserProfile");

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "lmsUserProfile",
                type: "INTEGER",
                nullable: true);
        }
    }
}
