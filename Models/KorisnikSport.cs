using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Matchletic.Models
{
    public class KorisnikSport
    {
        [Key]
        public int KorisnikSportID { get; set; }

        public int KorisnikID { get; set; }

        public int SportID { get; set; }

        [ForeignKey("KorisnikID")]
        public virtual Korisnik Korisnik { get; set; }

        [ForeignKey("SportID")]
        public virtual Sport Sport { get; set; }
    }
}
