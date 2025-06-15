using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Matchletic.Models
{
    public class StatistikaMeca
    {
        [Key]
        public int StatistikaID { get; set; }

        public int MecID { get; set; }

        public string Rezultat { get; set; }

        public string Pobjednik { get; set; }

        [ForeignKey("MecID")]
        public virtual Mec Mec { get; set; }
    }
}
