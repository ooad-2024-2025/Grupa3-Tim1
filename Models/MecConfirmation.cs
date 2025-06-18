using System;
using System.ComponentModel.DataAnnotations;

namespace Matchletic.Models
{
    public class MecConfirmation
    {
        [Key]
        public int MecConfirmationID { get; set; }

        [Required]
        public int MecID { get; set; }
        public Mec Mec { get; set; }

        [Required]
        public int KorisnikID { get; set; }
        public Korisnik Korisnik { get; set; }

        [Required(ErrorMessage = "Morate odabrati status.")]
        public bool IsWinner { get; set; }

        public DateTime ConfirmedAt { get; set; }
    }
}
