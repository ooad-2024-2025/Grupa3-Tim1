using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Matchletic.Models
{
    public class Postignuce
    {
        [Key]
        public int PostignuceID { get; set; }

        public string Naziv { get; set; }

        public string Opis { get; set; }

        public string Ikona { get; set; }

        // Tip ikone koji se koristi za feather icons
        public string IkonaTip { get; set; }

        // Boja pozadine za ikonu dostignuća
        public string BojaKlasa { get; set; }

        // Trag napretka 0-100%
        public int NapredakProcenti { get; set; }

        // Datum kada je dostignuće otključano, null ako nije otključano
        public DateTime? DatumOtkljucavanja { get; set; }

        // Veza sa korisnicima koji su dobili ovo dostignuće
        public virtual ICollection<KorisnikPostignuce> KorisniciPostignuca { get; set; }

        public Postignuce()
        {
            KorisniciPostignuca = new List<KorisnikPostignuce>();
        }
    }


    public class KorisnikPostignuce
    {
        [Key]
        public int KorisnikPostignuceID { get; set; }

        public int KorisnikID { get; set; }

        public int PostignuceID { get; set; }

        public int NapredakProcenti { get; set; }

        public DateTime? DatumOtkljucavanja { get; set; }

        [ForeignKey("KorisnikID")]
        public virtual Korisnik Korisnik { get; set; }

        [ForeignKey("PostignuceID")]
        public virtual Postignuce Postignuce { get; set; }
    }
}
