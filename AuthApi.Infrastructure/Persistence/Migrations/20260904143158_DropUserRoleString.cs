using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropUserRoleString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_roles_UserId_RoleId_AppId_CompanyId",
                table: "user_roles");

            migrationBuilder.Sql("""
                UPDATE user_roles ur
                SET "IsDeleted" = false,
                    "UpdatedAt" = NOW()
                FROM users u
                INNER JOIN LATERAL (
                    SELECT r."Id" AS "RoleId"
                    FROM roles r
                    WHERE r."IsDeleted" = false
                      AND r."Code" = CASE
                          WHEN u."Role" IN ('SuperAdmin', 'Admin', 'Operator', 'Viewer', 'User') THEN u."Role"
                          ELSE 'Viewer'
                      END
                      AND (r."CompanyId" IS NULL OR r."CompanyId" = u."CompanyId")
                    ORDER BY CASE
                        WHEN u."CompanyId" IS NOT NULL AND r."CompanyId" = u."CompanyId" THEN 0
                        WHEN r."CompanyId" IS NULL THEN 1
                        ELSE 2
                    END
                    LIMIT 1
                ) mapped ON true
                WHERE ur."UserId" = u."Id"
                  AND ur."RoleId" = mapped."RoleId"
                  AND ur."AppId" IS NULL
                  AND ur."IsDeleted" = true;

                INSERT INTO user_roles ("Id", "UserId", "RoleId", "CompanyId", "IsDeleted", "CreatedAt", "UpdatedAt")
                SELECT uuid_in(md5(random()::text || u."Id"::text || clock_timestamp()::text)::cstring),
                       u."Id", mapped."RoleId", u."CompanyId", false, NOW(), NOW()
                FROM users u
                INNER JOIN LATERAL (
                    SELECT r."Id" AS "RoleId"
                    FROM roles r
                    WHERE r."IsDeleted" = false
                      AND r."Code" = CASE
                          WHEN u."Role" IN ('SuperAdmin', 'Admin', 'Operator', 'Viewer', 'User') THEN u."Role"
                          ELSE 'Viewer'
                      END
                      AND (r."CompanyId" IS NULL OR r."CompanyId" = u."CompanyId")
                    ORDER BY CASE
                        WHEN u."CompanyId" IS NOT NULL AND r."CompanyId" = u."CompanyId" THEN 0
                        WHEN r."CompanyId" IS NULL THEN 1
                        ELSE 2
                    END
                    LIMIT 1
                ) mapped ON true
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM user_roles ur
                    WHERE ur."UserId" = u."Id"
                      AND ur."RoleId" = mapped."RoleId"
                      AND ur."AppId" IS NULL
                      AND ur."IsDeleted" = false
                );
                """);

            migrationBuilder.DropColumn(
                name: "Role",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_UserId_RoleId_AppId_CompanyId",
                table: "user_roles",
                columns: new[] { "UserId", "RoleId", "AppId", "CompanyId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_user_roles_UserId_RoleId_AppId_CompanyId",
                table: "user_roles");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Viewer");

            migrationBuilder.Sql("""
                UPDATE users u
                SET "Role" = COALESCE((
                    SELECT r."Code"
                    FROM user_roles ur
                    INNER JOIN roles r ON r."Id" = ur."RoleId"
                    WHERE ur."UserId" = u."Id"
                      AND ur."AppId" IS NULL
                      AND ur."IsDeleted" = false
                    ORDER BY ur."CreatedAt"
                    LIMIT 1
                ), 'Viewer');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_UserId_RoleId_AppId_CompanyId",
                table: "user_roles",
                columns: new[] { "UserId", "RoleId", "AppId", "CompanyId" },
                unique: true);
        }
    }
}
