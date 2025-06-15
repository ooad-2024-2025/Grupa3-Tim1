using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Matchletic.Models
{
    public class MecKorisnik
    {
        [Key]
        public int MecKorisnikID { get; set; }

        public int MecID { get; set; }

        public int KorisnikID { get; set; }

        public DateTime DatumMeca { get; set; }

        public bool JePrihvacen { get; set; } = false;  // Dodano polje

        // Navigation properties
        [ForeignKey("MecID")]
        public virtual Mec Mec { get; set; }

        [ForeignKey("KorisnikID")]
        public virtual Korisnik Korisnik { get; set; }

    }
}
