using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Matchletic.Models
{
    public class Recenzija
    {
        [Key]
        public int RecenzijaID { get; set; }

        [Range(1, 5)]
        public int Ocjena { get; set; }

        public string Komentar { get; set; }

        [BindNever]
        public int MecID { get; set; }

        [BindNever]
        public int AutorID { get; set; }

        // Navigation properties
        [ForeignKey("MecID")]
        public virtual Mec Mec { get; set; }

        [ForeignKey("AutorID")]
        public virtual Korisnik Autor { get; set; }

    }
}
