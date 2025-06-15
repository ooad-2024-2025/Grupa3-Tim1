using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matchletic.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOglasAndUpdateMec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KorisnickiSportovi_Sports_SportID",
                table: "KorisnickiSportovi");

            migrationBuilder.DropForeignKey(
                name: "FK_Mecevi_Oglasi_OglasID",
                table: "Mecevi");

            migrationBuilder.DropTable(
                name: "OglasiKorisnici");

            migrationBuilder.DropTable(
                name: "Oglasi");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Sports",
                table: "Sports");

            migrationBuilder.RenameTable(
                name: "Sports",
                newName: "Sportovi");

            migrationBuilder.RenameColumn(
                name: "PotrazivaciID",
                table: "Mecevi",
                newName: "SportID");

            migrationBuilder.RenameColumn(
                name: "OglasID",
                table: "Mecevi",
                newName: "KreatorID");

            migrationBuilder.RenameIndex(
                name: "IX_Mecevi_OglasID",
                table: "Mecevi",
                newName: "IX_Mecevi_KreatorID");

            migrationBuilder.AddColumn<int>(
                name: "BrojIgraca",
                table: "Mecevi",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatumKreiranja",
                table: "Mecevi",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Lokacija",
                table: "Mecevi",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Naslov",
                table: "Mecevi",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Opis",
                table: "Mecevi",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Sportovi",
                table: "Sportovi",
                column: "SportID");

            migrationBuilder.CreateIndex(
                name: "IX_Mecevi_SportID",
                table: "Mecevi",
                column: "SportID");

            migrationBuilder.AddForeignKey(
                name: "FK_KorisnickiSportovi_Sportovi_SportID",
                table: "KorisnickiSportovi",
                column: "SportID",
                principalTable: "Sportovi",
                principalColumn: "SportID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Mecevi_Korisnici_KreatorID",
                table: "Mecevi",
                column: "KreatorID",
                principalTable: "Korisnici",
                principalColumn: "KorisnikID",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Mecevi_Sportovi_SportID",
                table: "Mecevi",
                column: "SportID",
                principalTable: "Sportovi",
                principalColumn: "SportID",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KorisnickiSportovi_Sportovi_SportID",
                table: "KorisnickiSportovi");

            migrationBuilder.DropForeignKey(
                name: "FK_Mecevi_Korisnici_KreatorID",
                table: "Mecevi");

            migrationBuilder.DropForeignKey(
                name: "FK_Mecevi_Sportovi_SportID",
                table: "Mecevi");

            migrationBuilder.DropIndex(
                name: "IX_Mecevi_SportID",
                table: "Mecevi");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Sportovi",
                table: "Sportovi");

            migrationBuilder.DropColumn(
                name: "BrojIgraca",
                table: "Mecevi");

            migrationBuilder.DropColumn(
                name: "DatumKreiranja",
                table: "Mecevi");

            migrationBuilder.DropColumn(
                name: "Lokacija",
                table: "Mecevi");

            migrationBuilder.DropColumn(
                name: "Naslov",
                table: "Mecevi");

            migrationBuilder.DropColumn(
                name: "Opis",
                table: "Mecevi");

            migrationBuilder.RenameTable(
                name: "Sportovi",
                newName: "Sports");

            migrationBuilder.RenameColumn(
                name: "SportID",
                table: "Mecevi",
                newName: "PotrazivaciID");

            migrationBuilder.RenameColumn(
                name: "KreatorID",
                table: "Mecevi",
                newName: "OglasID");

            migrationBuilder.RenameIndex(
                name: "IX_Mecevi_KreatorID",
                table: "Mecevi",
                newName: "IX_Mecevi_OglasID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Sports",
                table: "Sports",
                column: "SportID");

            migrationBuilder.CreateTable(
                name: "Oglasi",
                columns: table => new
                {
                    OglasID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naslov = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Opis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Oglasi", x => x.OglasID);
                });

            migrationBuilder.CreateTable(
                name: "OglasiKorisnici",
                columns: table => new
                {
                    OglasKorisnikID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikID = table.Column<int>(type: "int", nullable: false),
                    OglasID = table.Column<int>(type: "int", nullable: false),
                    DatumObjave = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OglasiKorisnici", x => x.OglasKorisnikID);
                    table.ForeignKey(
                        name: "FK_OglasiKorisnici_Korisnici_KorisnikID",
                        column: x => x.KorisnikID,
                        principalTable: "Korisnici",
                        principalColumn: "KorisnikID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OglasiKorisnici_Oglasi_OglasID",
                        column: x => x.OglasID,
                        principalTable: "Oglasi",
                        principalColumn: "OglasID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OglasiKorisnici_KorisnikID",
                table: "OglasiKorisnici",
                column: "KorisnikID");

            migrationBuilder.CreateIndex(
                name: "IX_OglasiKorisnici_OglasID",
                table: "OglasiKorisnici",
                column: "OglasID");

            migrationBuilder.AddForeignKey(
                name: "FK_KorisnickiSportovi_Sports_SportID",
                table: "KorisnickiSportovi",
                column: "SportID",
                principalTable: "Sports",
                principalColumn: "SportID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Mecevi_Oglasi_OglasID",
                table: "Mecevi",
                column: "OglasID",
                principalTable: "Oglasi",
                principalColumn: "OglasID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
