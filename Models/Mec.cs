// Models/Mec.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Matchletic.Models
{
    public class Mec
    {
        public Mec()
        {
            KorisniciMeca = new List<MecKorisnik>();
            Recenzije = new List<Recenzija>();
        }

        [Key]
        public int MecID { get; set; }

        // Properties from Oglas
        [Required]
        public string Naslov { get; set; }

        public string Opis { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime DatumKreiranja { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime DatumMeca { get; set; }

        public StatusMeca Status { get; set; }

        public string Lokacija { get; set; }

        public int BrojIgraca { get; set; }

        public int SportID { get; set; }

        public int KreatorID { get; set; }

        public string Rezultat { get; set; } = string.Empty; // Inicijalizacija na prazni string

        public bool JePrivatan { get; set; } = false;

        // Navigation properties
        [ForeignKey("SportID")]
        public virtual Sport Sport { get; set; }

        [ForeignKey("KreatorID")]
        public virtual Korisnik Kreator { get; set; }

        public virtual ICollection<MecKorisnik> KorisniciMeca { get; set; }

        public virtual ICollection<Recenzija> Recenzije { get; set; }
        public ICollection<MecConfirmation> MecConfirmations { get; set; } = new List<MecConfirmation>();
    }
}
