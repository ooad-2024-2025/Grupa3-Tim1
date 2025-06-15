using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace Matchletic.Models
{
    public class Korisnik
    {
        // Dodajte ova polja u postojeći Korisnik model
        public string ProfilnaSlika { get; set; }
        public double Ocjena { get; set; } = 0.0;
        public bool JeAdmin { get; set; } = false;

        // Novo polje za lokaciju
        [Required]
        public string Lokacija { get; set; } = string.Empty;

        // Dodajte ovu navigation property
        public virtual ICollection<KorisnikSport> KorisnickiSportovi { get; set; }

        // Dodana navigacijska svojstva za Postignuca
        public virtual ICollection<KorisnikPostignuce> KorisnickaPostignuca { get; set; }

        public virtual ICollection<Notifikacija> Notifikacije { get; set; } = new List<Notifikacija>();

        public Korisnik()
        {
            // Inicijalizacija kolekcija u konstruktoru
            MeceviKorisnika = new List<MecKorisnik>();
            NapisaneRecenzije = new List<Recenzija>();
            KorisnickiSportovi = new List<KorisnikSport>();
            KorisnickaPostignuca = new List<KorisnikPostignuce>(); // Dodajemo inicijalizaciju
            Notifikacije = new List<Notifikacija>();
        }

        [Key]
        public int KorisnikID { get; set; }

        [Required]
        public string Lozinka { get; set; }

        [Required]
        public string Ime { get; set; }

        [Required]
        public string Prezime { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public Uloga Uloga { get; set; }

        public bool Aktivan { get; set; }

        // Navigation properties
        [BindNever]
        public virtual ICollection<MecKorisnik> MeceviKorisnika { get; set; }

        [BindNever]
        public virtual ICollection<Recenzija> NapisaneRecenzije { get; set; }
    }
}
