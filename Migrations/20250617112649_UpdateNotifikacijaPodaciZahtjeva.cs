using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matchletic.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNotifikacijaPodaciZahtjeva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PodaciZahtjeva",
                table: "Notifikacije",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PodaciZahtjeva",
                table: "Notifikacije",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldDefaultValue: "");
        }
    }
}
