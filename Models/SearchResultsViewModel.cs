using System.Collections.Generic;
using Matchletic.Models;

namespace Matchletic.Models
{
    public class SearchResultsViewModel
    {
        public string Query { get; set; }
        public List<Korisnik> Korisnici { get; set; } = new List<Korisnik>();
        public List<Mec> Mecevi { get; set; } = new List<Mec>();
    }
} 