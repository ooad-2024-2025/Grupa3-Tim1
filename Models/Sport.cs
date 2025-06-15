using System.ComponentModel.DataAnnotations;

namespace Matchletic.Models
{
    public class Sport
    {
        [Key]
        public int SportID { get; set; }

        [Required]
        public string Naziv { get; set; }

        public string Ikona { get; set; }

        // Navigation properties
        public virtual ICollection<KorisnikSport> KorisniciSportovi { get; set; }

        public Sport()
        {
            KorisniciSportovi = new List<KorisnikSport>();
        }
    }
}
