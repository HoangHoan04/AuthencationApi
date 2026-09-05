using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseIdentityEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EmailVerifiedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastLoginAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Locale",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MfaEnabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PasswordChangedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PhoneVerifiedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Timezone",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserType",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Human");

            migrationBuilder.AddColumn<Guid>(
                name: "AppId",
                table: "refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConsumedAt",
                table: "password_resets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtpHash",
                table: "password_resets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE password_resets
                SET "OtpHash" = "OtpCode"
                WHERE "OtpCode" IS NOT NULL AND BTRIM("OtpCode") <> '';
                """);

            migrationBuilder.DropColumn(
                name: "OtpCode",
                table: "password_resets");

            migrationBuilder.AddColumn<string>(
                name: "RequestIp",
                table: "password_resets",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AppId",
                table: "login_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "login_history",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeoCountry",
                table: "login_history",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.Sql("""
                ALTER TABLE ecosystem_apps
                ALTER COLUMN "RedirectUrlsJson" TYPE jsonb
                USING (
                    CASE
                        WHEN "RedirectUrlsJson" IS NULL OR BTRIM("RedirectUrlsJson") = '' THEN NULL
                        WHEN LEFT(BTRIM("RedirectUrlsJson"), 1) IN ('{', '[', '"') THEN "RedirectUrlsJson"::jsonb
                        ELSE to_jsonb("RedirectUrlsJson")
                    END
                );
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ClientId",
                table: "ecosystem_apps",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccessTokenTtlMinutes",
                table: "ecosystem_apps",
                type: "integer",
                nullable: false,
                defaultValue: 15);

            migrationBuilder.AddColumn<string>(
                name: "AllowedOriginsJson",
                table: "ecosystem_apps",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppType",
                table: "ecosystem_apps",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Spa");

            migrationBuilder.AddColumn<string>(
                name: "ClientSecretHash",
                table: "ecosystem_apps",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE ecosystem_apps
                SET "ClientSecretHash" = "ClientSecret"
                WHERE "ClientSecret" IS NOT NULL AND BTRIM("ClientSecret") <> '';
                """);

            migrationBuilder.DropColumn(
                name: "ClientSecret",
                table: "ecosystem_apps");

            migrationBuilder.AddColumn<string>(
                name: "GrantTypesJson",
                table: "ecosystem_apps",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefreshTokenTtlDays",
                table: "ecosystem_apps",
                type: "integer",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<bool>(
                name: "RequirePkce",
                table: "ecosystem_apps",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ScopesJson",
                table: "ecosystem_apps",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SecretLastRotatedAt",
                table: "ecosystem_apps",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "companies",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "VN");

            migrationBuilder.AddColumn<int>(
                name: "MaxUsers",
                table: "companies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentCompanyId",
                table: "companies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanTier",
                table: "companies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProvinceId",
                table: "companies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettingsJson",
                table: "companies",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WardId",
                table: "companies",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BeforeJson = table.Column<string>(type: "jsonb", nullable: true),
                    AfterJson = table.Column<string>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "auth_client_secrets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecretHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SecretPrefix = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_client_secrets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_auth_client_secrets_ecosystem_apps_AppId",
                        column: x => x.AppId,
                        principalTable: "ecosystem_apps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "authorization_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RedirectUri = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CodeChallenge = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CodeChallengeMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Scope = table.Column<string>(type: "text", nullable: true),
                    Nonce = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authorization_codes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_authorization_codes_ecosystem_apps_AppId",
                        column: x => x.AppId,
                        principalTable: "ecosystem_apps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_authorization_codes_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "email_verifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_verifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_email_verifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mfa_backup_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mfa_backup_codes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mfa_backup_codes_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mfa_devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SecretEncrypted = table.Column<string>(type: "text", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mfa_devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_mfa_devices_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "password_histories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_histories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_password_histories_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Resource = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_roles_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "signing_keys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KeyId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Algorithm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Use = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PrivateKeyPemEncrypted = table.Column<string>(type: "text", nullable: false),
                    PublicKeyPem = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RotatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetireAfter = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signing_keys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "token_denylist",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Jti = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_token_denylist", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_apps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_apps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_apps_ecosystem_apps_AppId",
                        column: x => x.AppId,
                        principalTable: "ecosystem_apps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_apps_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_roles_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_ecosystem_apps_AppId",
                        column: x => x.AppId,
                        principalTable: "ecosystem_apps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_roles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_AppId",
                table: "refresh_tokens",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_login_history_AppId",
                table: "login_history",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_login_history_CreatedAt",
                table: "login_history",
                column: "CreatedAt");

            migrationBuilder.Sql("""
                UPDATE ecosystem_apps
                SET "ClientId" = NULL
                WHERE "ClientId" IS NOT NULL AND BTRIM("ClientId") = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ecosystem_apps_ClientId",
                table: "ecosystem_apps",
                column: "ClientId",
                unique: true,
                filter: "\"ClientId\" IS NOT NULL AND \"ClientId\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_companies_ParentCompanyId",
                table: "companies",
                column: "ParentCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_companies_ProvinceId",
                table: "companies",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_companies_WardId",
                table: "companies",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_CreatedAt",
                table: "audit_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_EntityType_EntityId",
                table: "audit_logs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_auth_client_secrets_AppId",
                table: "auth_client_secrets",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_authorization_codes_AppId",
                table: "authorization_codes",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_authorization_codes_CodeHash",
                table: "authorization_codes",
                column: "CodeHash");

            migrationBuilder.CreateIndex(
                name: "IX_authorization_codes_ExpiresAt",
                table: "authorization_codes",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_authorization_codes_UserId",
                table: "authorization_codes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_email_verifications_UserId",
                table: "email_verifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_mfa_backup_codes_UserId",
                table: "mfa_backup_codes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_mfa_devices_UserId",
                table: "mfa_devices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_password_histories_UserId",
                table: "password_histories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_Code",
                table: "permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_PermissionId",
                table: "role_permissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_RoleId_PermissionId",
                table: "role_permissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_CompanyId_Code",
                table: "roles",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_signing_keys_KeyId",
                table: "signing_keys",
                column: "KeyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_token_denylist_ExpiresAt",
                table: "token_denylist",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_token_denylist_Jti",
                table: "token_denylist",
                column: "Jti",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_apps_AppId",
                table: "user_apps",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_user_apps_UserId_AppId",
                table: "user_apps",
                columns: new[] { "UserId", "AppId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_AppId",
                table: "user_roles",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_CompanyId",
                table: "user_roles",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_RoleId",
                table: "user_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_UserId_RoleId_AppId_CompanyId",
                table: "user_roles",
                columns: new[] { "UserId", "RoleId", "AppId", "CompanyId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_companies_companies_ParentCompanyId",
                table: "companies",
                column: "ParentCompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_companies_provinces_ProvinceId",
                table: "companies",
                column: "ProvinceId",
                principalTable: "provinces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_companies_wards_WardId",
                table: "companies",
                column: "WardId",
                principalTable: "wards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_login_history_ecosystem_apps_AppId",
                table: "login_history",
                column: "AppId",
                principalTable: "ecosystem_apps",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_ecosystem_apps_AppId",
                table: "refresh_tokens",
                column: "AppId",
                principalTable: "ecosystem_apps",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_companies_companies_ParentCompanyId",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_companies_provinces_ProvinceId",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_companies_wards_WardId",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_login_history_ecosystem_apps_AppId",
                table: "login_history");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_ecosystem_apps_AppId",
                table: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "auth_client_secrets");

            migrationBuilder.DropTable(
                name: "authorization_codes");

            migrationBuilder.DropTable(
                name: "email_verifications");

            migrationBuilder.DropTable(
                name: "mfa_backup_codes");

            migrationBuilder.DropTable(
                name: "mfa_devices");

            migrationBuilder.DropTable(
                name: "password_histories");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "signing_keys");

            migrationBuilder.DropTable(
                name: "token_denylist");

            migrationBuilder.DropTable(
                name: "user_apps");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_AppId",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_login_history_AppId",
                table: "login_history");

            migrationBuilder.DropIndex(
                name: "IX_login_history_CreatedAt",
                table: "login_history");

            migrationBuilder.DropIndex(
                name: "IX_ecosystem_apps_ClientId",
                table: "ecosystem_apps");

            migrationBuilder.DropIndex(
                name: "IX_companies_ParentCompanyId",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "IX_companies_ProvinceId",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "IX_companies_WardId",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Locale",
                table: "users");

            migrationBuilder.DropColumn(
                name: "MfaEnabled",
                table: "users");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordChangedAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PhoneVerifiedAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Timezone",
                table: "users");

            migrationBuilder.DropColumn(
                name: "UserType",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AppId",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "ConsumedAt",
                table: "password_resets");

            migrationBuilder.DropColumn(
                name: "OtpHash",
                table: "password_resets");

            migrationBuilder.DropColumn(
                name: "RequestIp",
                table: "password_resets");

            migrationBuilder.DropColumn(
                name: "AppId",
                table: "login_history");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "login_history");

            migrationBuilder.DropColumn(
                name: "GeoCountry",
                table: "login_history");

            migrationBuilder.DropColumn(
                name: "AccessTokenTtlMinutes",
                table: "ecosystem_apps");

            migrationBuilder.DropColumn(
                name: "AllowedOriginsJson",
                table: "ecosystem_apps");

            migrationBuilder.DropColumn(
                name: "AppType",
                table: "ecosystem_apps");

            migrationBuilder.DropColumn(
                name: "ClientSecretHash",
                table: "ecosystem_apps");

            migrationBuilder.DropColumn(
                name: "GrantTypesJson",
                table: "ecosystem_apps");

            migrationBuilder.DropColumn(
                name: "RefreshTokenTtlDays",
                table: "ecosystem_apps");

            migrationBuilder.DropColumn(
                name: "RequirePkce",
                table: "ecosystem_apps");

            migrationBuilder.DropColumn(
                name: "ScopesJson",
                table: "ecosystem_apps");

            migrationBuilder.DropColumn(
                name: "SecretLastRotatedAt",
                table: "ecosystem_apps");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "MaxUsers",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "ParentCompanyId",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "PlanTier",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "ProvinceId",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "SettingsJson",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "WardId",
                table: "companies");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "OtpCode",
                table: "password_resets",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RedirectUrlsJson",
                table: "ecosystem_apps",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClientId",
                table: "ecosystem_apps",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientSecret",
                table: "ecosystem_apps",
                type: "text",
                nullable: true);
        }
    }
}
