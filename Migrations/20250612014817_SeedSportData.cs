using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Matchletic.Migrations
{
    /// <inheritdoc />
    public partial class SeedSportData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KorisnikSport_Korisnici_KorisnikID",
                table: "KorisnikSport");

            migrationBuilder.DropForeignKey(
                name: "FK_KorisnikSport_Sports_SportID",
                table: "KorisnikSport");

            migrationBuilder.DropPrimaryKey(
                name: "PK_KorisnikSport",
                table: "KorisnikSport");

            migrationBuilder.RenameTable(
                name: "KorisnikSport",
                newName: "KorisnickiSportovi");

            migrationBuilder.RenameIndex(
                name: "IX_KorisnikSport_SportID",
                table: "KorisnickiSportovi",
                newName: "IX_KorisnickiSportovi_SportID");

            migrationBuilder.RenameIndex(
                name: "IX_KorisnikSport_KorisnikID",
                table: "KorisnickiSportovi",
                newName: "IX_KorisnickiSportovi_KorisnikID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_KorisnickiSportovi",
                table: "KorisnickiSportovi",
                column: "KorisnikSportID");

            migrationBuilder.InsertData(
                table: "Sports",
                columns: new[] { "SportID", "Ikona", "Naziv" },
                values: new object[,]
                {
                    { 1, "⚽", "Nogomet" },
                    { 2, "🏀", "Košarka" },
                    { 3, "🎾", "Tenis" },
                    { 4, "⛳", "Golf" },
                    { 5, "🏊", "Plivanje" },
                    { 6, "🏐", "Odbojka" },
                    { 7, "⚾", "Bejzbol" },
                    { 8, "🏉", "Ragbi" },
                    { 9, "🏏", "Kriket" },
                    { 10, "🥊", "Boks" }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_KorisnickiSportovi_Korisnici_KorisnikID",
                table: "KorisnickiSportovi",
                column: "KorisnikID",
                principalTable: "Korisnici",
                principalColumn: "KorisnikID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KorisnickiSportovi_Sports_SportID",
                table: "KorisnickiSportovi",
                column: "SportID",
                principalTable: "Sports",
                principalColumn: "SportID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KorisnickiSportovi_Korisnici_KorisnikID",
                table: "KorisnickiSportovi");

            migrationBuilder.DropForeignKey(
                name: "FK_KorisnickiSportovi_Sports_SportID",
                table: "KorisnickiSportovi");

            migrationBuilder.DropPrimaryKey(
                name: "PK_KorisnickiSportovi",
                table: "KorisnickiSportovi");

            migrationBuilder.DeleteData(
                table: "Sports",
                keyColumn: "SportID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Sports",
                keyColumn: "SportID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Sports",
                keyColumn: "SportID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Sports",
                keyColumn: "SportID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Sports",
                keyColumn: "SportID",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Sports",
                keyColumn: "SportID",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Sports",
                keyColumn: "SportID",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Sports",
                keyColumn: "SportID",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Sports",
                keyColumn: "SportID",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Sports",
                keyColumn: "SportID",
                keyValue: 10);

            migrationBuilder.RenameTable(
                name: "KorisnickiSportovi",
                newName: "KorisnikSport");

            migrationBuilder.RenameIndex(
                name: "IX_KorisnickiSportovi_SportID",
                table: "KorisnikSport",
                newName: "IX_KorisnikSport_SportID");

            migrationBuilder.RenameIndex(
                name: "IX_KorisnickiSportovi_KorisnikID",
                table: "KorisnikSport",
                newName: "IX_KorisnikSport_KorisnikID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_KorisnikSport",
                table: "KorisnikSport",
                column: "KorisnikSportID");

            migrationBuilder.AddForeignKey(
                name: "FK_KorisnikSport_Korisnici_KorisnikID",
                table: "KorisnikSport",
                column: "KorisnikID",
                principalTable: "Korisnici",
                principalColumn: "KorisnikID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KorisnikSport_Sports_SportID",
                table: "KorisnikSport",
                column: "SportID",
                principalTable: "Sports",
                principalColumn: "SportID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
