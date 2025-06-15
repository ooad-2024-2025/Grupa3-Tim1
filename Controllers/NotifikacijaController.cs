// Controllers/NotifikacijaController.cs
using System;
using System.Threading.Tasks;
using Matchletic.Data;
using Matchletic.Models;
using Matchletic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Matchletic.Controllers
{
    [Authorize]
    public class NotifikacijaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotifikacijaService _notifikacijaService;

        public NotifikacijaController(ApplicationDbContext context, NotifikacijaService notifikacijaService)
        {
            _context = context;
            _notifikacijaService = notifikacijaService;
        }

        public async Task<IActionResult> Index()
        {
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            var notifikacije = await _notifikacijaService.GetNotifikacijeZaKorisnikaAsync(korisnikID.Value, 50);
            return View(notifikacije);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OznaciKaoProcitano(int id, string returnUrl = null)
        {
            await _notifikacijaService.OznaciKaoProcitanoAsync(id);

            if (!string.IsNullOrEmpty(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OznaciSveKaoProcitano(string returnUrl = null)
        {
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            await _notifikacijaService.OznaciSveKaoProcitanoAsync(korisnikID.Value);

            if (!string.IsNullOrEmpty(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }
    }
}
