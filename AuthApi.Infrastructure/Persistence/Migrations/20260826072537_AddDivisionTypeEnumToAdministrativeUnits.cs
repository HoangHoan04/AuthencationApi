using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthApi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDivisionTypeEnumToAdministrativeUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DivisionType",
                table: "wards",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Commune");

            migrationBuilder.AlterColumn<string>(
                name: "AdministrativeRegion",
                table: "provinces",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DivisionType",
                table: "provinces",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Province");

            migrationBuilder.AddColumn<string>(
                name: "OtpCode",
                table: "password_resets",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DivisionType",
                table: "wards");

            migrationBuilder.DropColumn(
                name: "DivisionType",
                table: "provinces");

            migrationBuilder.DropColumn(
                name: "OtpCode",
                table: "password_resets");

            migrationBuilder.AlterColumn<string>(
                name: "AdministrativeRegion",
                table: "provinces",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
