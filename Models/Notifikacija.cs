// Models/Notifikacija.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Matchletic.Models
{
    public class Notifikacija
    {
        [Key]
        public int NotifikacijaID { get; set; }

        public int KorisnikID { get; set; }

        [Required]
        public string Naslov { get; set; }

        [Required]
        public string Sadrzaj { get; set; }

        public string Url { get; set; }

        public string PodaciZahtjeva { get; set; } = "";

        public NotifikacijaTip Tip { get; set; }

        public bool Procitano { get; set; } = false;

        public DateTime DatumKreiranja { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("KorisnikID")]
        public virtual Korisnik Korisnik { get; set; }
    }
}
