// ViewComponents/SidebarViewComponent.cs
using System.Threading.Tasks;
using Matchletic.Data;
using Matchletic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Matchletic.ViewComponents
{
    public class SidebarViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public SidebarViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Dohvati ID prijavljenog korisnika
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");

            // Dohvati broj zahtjeva za mečeve
            if (korisnikID.HasValue)
            {
                var brojZahtjeva = await _context.MecRequests
                    .CountAsync(r => r.Mec.KreatorID == korisnikID && r.Status == MecRequestStatus.Ceka);

                ViewBag.BrojZahtjeva = brojZahtjeva;
            }

            return View();
        }
    }
}
