using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsPortalBe.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lmsNotification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    ActorId = table.Column<string>(type: "TEXT", nullable: true),
                    CourseId = table.Column<int>(type: "INTEGER", nullable: true),
                    ModuleId = table.Column<int>(type: "INTEGER", nullable: true),
                    ActivityId = table.Column<int>(type: "INTEGER", nullable: true),
                    ResourceId = table.Column<int>(type: "INTEGER", nullable: true),
                    SubmissionId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lmsNotification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lmsNotification_lmsUser_ActorId",
                        column: x => x.ActorId,
                        principalTable: "lmsUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "lmsUserNotification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    NotificationId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsSeen = table.Column<bool>(type: "INTEGER", nullable: false),
                    SeenAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lmsUserNotification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lmsUserNotification_lmsNotification_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "lmsNotification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lmsUserNotification_lmsUser_UserId",
                        column: x => x.UserId,
                        principalTable: "lmsUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lmsNotification_ActorId",
                table: "lmsNotification",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_lmsNotification_CreatedAt",
                table: "lmsNotification",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_lmsUserNotification_NotificationId",
                table: "lmsUserNotification",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_lmsUserNotification_UserId_IsSeen",
                table: "lmsUserNotification",
                columns: new[] { "UserId", "IsSeen" });

            migrationBuilder.CreateIndex(
                name: "IX_lmsUserNotification_UserId_NotificationId",
                table: "lmsUserNotification",
                columns: new[] { "UserId", "NotificationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lmsUserNotification");

            migrationBuilder.DropTable(
                name: "lmsNotification");
        }
    }
}
