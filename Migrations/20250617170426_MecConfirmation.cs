using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matchletic.Migrations
{
    /// <inheritdoc />
    public partial class MecConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MecConfirmation",
                columns: table => new
                {
                    MecConfirmationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MecID = table.Column<int>(type: "int", nullable: false),
                    KorisnikID = table.Column<int>(type: "int", nullable: false),
                    IsWinner = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MecConfirmation", x => x.MecConfirmationID);
                    table.ForeignKey(
                        name: "FK_MecConfirmation_Korisnici_KorisnikID",
                        column: x => x.KorisnikID,
                        principalTable: "Korisnici",
                        principalColumn: "KorisnikID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MecConfirmation_Mecevi_MecID",
                        column: x => x.MecID,
                        principalTable: "Mecevi",
                        principalColumn: "MecID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MecConfirmation_KorisnikID",
                table: "MecConfirmation",
                column: "KorisnikID");

            migrationBuilder.CreateIndex(
                name: "IX_MecConfirmation_MecID",
                table: "MecConfirmation",
                column: "MecID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MecConfirmation");
        }
    }
}
