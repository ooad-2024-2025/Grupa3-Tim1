using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matchletic.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMecConfirmationRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MecConfirmation_Korisnici_KorisnikID",
                table: "MecConfirmation");

            migrationBuilder.DropForeignKey(
                name: "FK_MecConfirmation_Mecevi_MecID",
                table: "MecConfirmation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MecConfirmation",
                table: "MecConfirmation");

            migrationBuilder.RenameTable(
                name: "MecConfirmation",
                newName: "MecConfirmations");

            migrationBuilder.RenameIndex(
                name: "IX_MecConfirmation_MecID",
                table: "MecConfirmations",
                newName: "IX_MecConfirmations_MecID");

            migrationBuilder.RenameIndex(
                name: "IX_MecConfirmation_KorisnikID",
                table: "MecConfirmations",
                newName: "IX_MecConfirmations_KorisnikID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MecConfirmations",
                table: "MecConfirmations",
                column: "MecConfirmationID");

            migrationBuilder.AddForeignKey(
                name: "FK_MecConfirmations_Korisnici_KorisnikID",
                table: "MecConfirmations",
                column: "KorisnikID",
                principalTable: "Korisnici",
                principalColumn: "KorisnikID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MecConfirmations_Mecevi_MecID",
                table: "MecConfirmations",
                column: "MecID",
                principalTable: "Mecevi",
                principalColumn: "MecID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MecConfirmations_Korisnici_KorisnikID",
                table: "MecConfirmations");

            migrationBuilder.DropForeignKey(
                name: "FK_MecConfirmations_Mecevi_MecID",
                table: "MecConfirmations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MecConfirmations",
                table: "MecConfirmations");

            migrationBuilder.RenameTable(
                name: "MecConfirmations",
                newName: "MecConfirmation");

            migrationBuilder.RenameIndex(
                name: "IX_MecConfirmations_MecID",
                table: "MecConfirmation",
                newName: "IX_MecConfirmation_MecID");

            migrationBuilder.RenameIndex(
                name: "IX_MecConfirmations_KorisnikID",
                table: "MecConfirmation",
                newName: "IX_MecConfirmation_KorisnikID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MecConfirmation",
                table: "MecConfirmation",
                column: "MecConfirmationID");

            migrationBuilder.AddForeignKey(
                name: "FK_MecConfirmation_Korisnici_KorisnikID",
                table: "MecConfirmation",
                column: "KorisnikID",
                principalTable: "Korisnici",
                principalColumn: "KorisnikID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MecConfirmation_Mecevi_MecID",
                table: "MecConfirmation",
                column: "MecID",
                principalTable: "Mecevi",
                principalColumn: "MecID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
