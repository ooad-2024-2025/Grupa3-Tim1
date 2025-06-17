using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Matchletic.Models;

namespace Matchletic.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Korisnik> Korisnici { get; set; }
        public DbSet<Mec> Mecevi { get; set; }
        public DbSet<Recenzija> Recenzije { get; set; }
        public DbSet<MecKorisnik> MeceviKorisnici { get; set; }
        public DbSet<Sport> Sportovi { get; set; }
        public DbSet<KorisnikSport> KorisnickiSportovi { get; set; }
        public DbSet<Notifikacija> Notifikacije { get; set; }
        public DbSet<MecRequest> MecRequests { get; set; }
        public DbSet<Postignuce> Postignuca { get; set; }
        public DbSet<KorisnikPostignuce> KorisnikPostignuca { get; set; }

        public DbSet<MecConfirmation> MecConfirmations { get; set; }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships for MecKorisnik
            modelBuilder.Entity<MecKorisnik>()
                .HasOne(mk => mk.Mec)
                .WithMany(m => m.KorisniciMeca)
                .HasForeignKey(mk => mk.MecID);

            modelBuilder.Entity<MecKorisnik>()
                .HasOne(mk => mk.Korisnik)
                .WithMany(k => k.MeceviKorisnika)
                .HasForeignKey(mk => mk.KorisnikID);

            // Configure MecConfirmation relationships
            modelBuilder.Entity<MecConfirmation>()
                .ToTable("MecConfirmations");

            modelBuilder.Entity<MecConfirmation>()
                .HasOne(mc => mc.Mec)
                .WithMany(m => m.MecConfirmations)
                .HasForeignKey(mc => mc.MecID);

            modelBuilder.Entity<MecConfirmation>()
                .HasOne(mc => mc.Korisnik)
                .WithMany()
                .HasForeignKey(mc => mc.KorisnikID);

            // Configure Recenzija relationships
            modelBuilder.Entity<Recenzija>()
                .HasOne(r => r.Mec)
                .WithMany(m => m.Recenzije)
                .HasForeignKey(r => r.MecID);

            modelBuilder.Entity<Recenzija>()
                .HasOne(r => r.Autor)
                .WithMany(k => k.NapisaneRecenzije)
                .HasForeignKey(r => r.AutorID);

            // Configure Mec foreign key to Sport
            modelBuilder.Entity<Mec>()
                .HasOne(m => m.Sport)
                .WithMany()
                .HasForeignKey(m => m.SportID);

            // Configure Mec foreign key to Korisnik (Creator)
            modelBuilder.Entity<Mec>()
                .HasOne(m => m.Kreator)
                .WithMany()
                .HasForeignKey(m => m.KreatorID);

            // Configure relationships for Postignuca
            modelBuilder.Entity<KorisnikPostignuce>()
                .HasOne(kp => kp.Korisnik)
                .WithMany(k => k.KorisnickaPostignuca)
                .HasForeignKey(kp => kp.KorisnikID);

            modelBuilder.Entity<KorisnikPostignuce>()
                .HasOne(kp => kp.Postignuce)
                .WithMany(p => p.KorisniciPostignuca)
                .HasForeignKey(kp => kp.PostignuceID);

            modelBuilder.Entity<Sport>().HasData(
            new Sport { SportID = 1, Naziv = "Nogomet", Ikona = "⚽" },
            new Sport { SportID = 2, Naziv = "Košarka", Ikona = "🏀" },
            new Sport { SportID = 3, Naziv = "Tenis", Ikona = "🎾" },
            new Sport { SportID = 4, Naziv = "Golf", Ikona = "⛳" },
            new Sport { SportID = 5, Naziv = "Plivanje", Ikona = "🏊" },
            new Sport { SportID = 6, Naziv = "Odbojka", Ikona = "🏐" },
            new Sport { SportID = 7, Naziv = "Bejzbol", Ikona = "⚾" },
            new Sport { SportID = 8, Naziv = "Ragbi", Ikona = "🏉" },
            new Sport { SportID = 9, Naziv = "Kriket", Ikona = "🏏" },
            new Sport { SportID = 10, Naziv = "Boks", Ikona = "🥊" }
            );
        }

        public async Task SeedPostignucaAsync()
        {
            if (!Postignuca.Any())
            {
                var dostignuca = new List<Postignuce>
        {
            new Postignuce
            {
                Naziv = "Prvi Meč",
                Opis = "Završite vaš prvi meč",
                Ikona = "🏆", // Dodano polje Ikona
                IkonaTip = "trophy",
                BojaKlasa = "bg-yellow-100 text-yellow-600"
            },
            new Postignuce
            {
                Naziv = "Niz Pobjeda",
                Opis = "Pobjedite u 5 mečeva zaredom",
                Ikona = "⚡", // Dodano polje Ikona
                IkonaTip = "zap",
                BojaKlasa = "bg-blue-100 text-blue-600"
            },
            new Postignuce
            {
                Naziv = "Društveni Leptir",
                Opis = "Igrajte sa 10 različitih igrača",
                Ikona = "👥", // Dodano polje Ikona
                IkonaTip = "users",
                BojaKlasa = "bg-purple-100 text-purple-600"
            },
            new Postignuce
            {
                Naziv = "Savršen Rezultat",
                Opis = "Pobjedite u meču bez gubitka poena",
                Ikona = "✅", // Dodano polje Ikona
                IkonaTip = "check-circle",
                BojaKlasa = "bg-green-100 text-green-600"
            },
            new Postignuce
            {
                Naziv = "Pobjednik Turnira",
                Opis = "Osvojite turnir",
                Ikona = "🏆", // Dodano polje Ikona
                IkonaTip = "award",
                BojaKlasa = "bg-gray-100 text-gray-600"
            },
            new Postignuce
            {
                Naziv = "Majstor Izazova",
                Opis = "Ispunite sve dnevne izazove za sedmicu",
                Ikona = "📊", // Dodano polje Ikona
                IkonaTip = "bar-chart-2",
                BojaKlasa = "bg-orange-100 text-orange-600"
            }
        };

                Postignuca.AddRange(dostignuca);
                await SaveChangesAsync();
            }
        }


    }
}
