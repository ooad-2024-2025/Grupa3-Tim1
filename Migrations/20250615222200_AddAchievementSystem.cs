using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matchletic.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievementSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Postignuca",
                columns: table => new
                {
                    PostignuceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naziv = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Opis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ikona = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IkonaTip = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BojaKlasa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NapredakProcenti = table.Column<int>(type: "int", nullable: false),
                    DatumOtkljucavanja = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Postignuca", x => x.PostignuceID);
                });

            migrationBuilder.CreateTable(
                name: "KorisnikPostignuca",
                columns: table => new
                {
                    KorisnikPostignuceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KorisnikID = table.Column<int>(type: "int", nullable: false),
                    PostignuceID = table.Column<int>(type: "int", nullable: false),
                    NapredakProcenti = table.Column<int>(type: "int", nullable: false),
                    DatumOtkljucavanja = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KorisnikPostignuca", x => x.KorisnikPostignuceID);
                    table.ForeignKey(
                        name: "FK_KorisnikPostignuca_Korisnici_KorisnikID",
                        column: x => x.KorisnikID,
                        principalTable: "Korisnici",
                        principalColumn: "KorisnikID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KorisnikPostignuca_Postignuca_PostignuceID",
                        column: x => x.PostignuceID,
                        principalTable: "Postignuca",
                        principalColumn: "PostignuceID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KorisnikPostignuca_KorisnikID",
                table: "KorisnikPostignuca",
                column: "KorisnikID");

            migrationBuilder.CreateIndex(
                name: "IX_KorisnikPostignuca_PostignuceID",
                table: "KorisnikPostignuca",
                column: "PostignuceID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KorisnikPostignuca");

            migrationBuilder.DropTable(
                name: "Postignuca");
        }
    }
}
