using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePartnerAndEcosystemAppFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "ecosystem_apps",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientSecret",
                table: "ecosystem_apps",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Namespace",
                table: "ecosystem_apps",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RedirectUrlsJson",
                table: "ecosystem_apps",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceName",
                table: "ecosystem_apps",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Logo",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                table: "companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "companies",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "ecosystem_apps");

            migrationBuilder.DropColumn(
                name: "ClientSecret",
                table: "ecosystem_apps");

            migrationBuilder.DropColumn(
                name: "Namespace",
                table: "ecosystem_apps");

            migrationBuilder.DropColumn(
                name: "RedirectUrlsJson",
                table: "ecosystem_apps");

            migrationBuilder.DropColumn(
                name: "ServiceName",
                table: "ecosystem_apps");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "Logo",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "TaxCode",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "companies");
        }
    }
}
