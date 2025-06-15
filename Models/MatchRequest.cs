// Models/MecRequest.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Matchletic.Models;

namespace Matchletic.Models
{
    public class MecRequest
    {
        [Key]
        public int MecRequestID { get; set; }

        public int MecID { get; set; }

        public int KorisnikID { get; set; }

        public string Status { get; set; } = "Ceka"; // Ceka, Prihvacen, Odbijen

        public DateTime DatumKreiranja { get; set; } = DateTime.Now;

        public DateTime? DatumAzuriranja { get; set; }

        // Navigation properties
        [ForeignKey("MecID")]
        public virtual Mec Mec { get; set; }

        [ForeignKey("KorisnikID")]
        public virtual Korisnik Korisnik { get; set; }
    }
}
