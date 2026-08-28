using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lmsPortalBe.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleRolePerUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Users that somehow ended up with more than one role (e.g. an
            // admin that was also assigned a student/teacher role) must be
            // reduced to a single role before the unique index can be added.
            // Priority: admin > teacher > student.
            migrationBuilder.Sql(
                """
                WITH RankedRoles AS
                (
                    SELECT
                        ur.UserId,
                        ur.RoleId,
                        ROW_NUMBER() OVER (
                            PARTITION BY ur.UserId
                            ORDER BY
                                CASE r.NormalizedName
                                    WHEN 'ADMIN' THEN 0
                                    WHEN 'TEACHER' THEN 1
                                    WHEN 'STUDENT' THEN 2
                                    ELSE 3
                                END
                        ) AS rn
                    FROM [lmsUser_Role] ur
                    INNER JOIN [lmsRole] r ON r.Id = ur.RoleId
                )
                DELETE ur
                FROM [lmsUser_Role] ur
                INNER JOIN RankedRoles rr
                    ON rr.UserId = ur.UserId AND rr.RoleId = ur.RoleId
                WHERE rr.rn > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_lmsUser_Role_UserId",
                table: "lmsUser_Role",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_lmsUser_Role_UserId",
                table: "lmsUser_Role");
        }
    }
}
