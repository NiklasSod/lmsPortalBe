using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsPortalBe.Migrations
{
    /// <inheritdoc />
    public partial class KeepSubmissionsOnAssignmentDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lmsSubmission_lmsAssignment_AssignmentId",
                table: "lmsSubmission");

            migrationBuilder.AlterColumn<int>(
                name: "AssignmentId",
                table: "lmsSubmission",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_lmsSubmission_lmsAssignment_AssignmentId",
                table: "lmsSubmission",
                column: "AssignmentId",
                principalTable: "lmsAssignment",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lmsSubmission_lmsAssignment_AssignmentId",
                table: "lmsSubmission");

            migrationBuilder.AlterColumn<int>(
                name: "AssignmentId",
                table: "lmsSubmission",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_lmsSubmission_lmsAssignment_AssignmentId",
                table: "lmsSubmission",
                column: "AssignmentId",
                principalTable: "lmsAssignment",
                principalColumn: "Id");
        }
    }
}
