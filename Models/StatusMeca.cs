// Models/StatusMeca.cs
namespace Matchletic.Models
{
    public enum StatusMeca
    {
        Objavljen,   // Match is published, waiting for player requests
        CekaPrihvacanje, // Challenge is waiting for acceptance
        Dogovoren,   // Match is arranged after creator accepts requests
        Zavrsen      // Match is completed
    }
}
