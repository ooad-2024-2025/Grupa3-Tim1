using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matchletic.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "JePrihvacen",
                table: "MeceviKorisnici",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JePrihvacen",
                table: "MeceviKorisnici");
        }
    }
}
