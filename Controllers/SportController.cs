// Controllers/SportController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Matchletic.Data;
using Matchletic.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Matchletic.Services;

namespace Matchletic.Controllers
{
    [Authorize]
    public class SportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserSyncService _userSyncService;
        private readonly UserManager<IdentityUser> _userManager;

        public SportController(
            ApplicationDbContext context,
            UserSyncService userSyncService,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userSyncService = userSyncService;
            _userManager = userManager;
        }

        // GET: Sport/Selector
        public async Task<IActionResult> Selector()
        {
            // Dohvati sve sportove
            var sportovi = await _context.Sportovi.ToListAsync();
            return View(sportovi);
        }

        // POST: Sport/SaveSelected
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSelected(int[] selectedSports)
        {
            if (selectedSports == null || !selectedSports.Any())
            {
                // Korisnik nije odabrao nijedan sport
                return RedirectToAction("Index", "Home");
            }

            // Get current user ID
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");

            // If session is not set but user is authenticated, try to sync and set it
            if (korisnikID == null && User.Identity.IsAuthenticated)
            {
                var email = User.Identity.Name;
                // Sync the user
                await _userSyncService.SyncIdentityUser(email);
                // Try to get the korisnikID again from the database
                var korisnik = await _context.Korisnici.FirstOrDefaultAsync(k => k.Email == email);
                if (korisnik != null)
                {
                    korisnikID = korisnik.KorisnikID;
                    HttpContext.Session.SetInt32("KorisnikID", korisnikID.Value);
                    HttpContext.Session.SetString("KorisnikIme", korisnik.Ime);
                }
                else
                {
                    return Challenge();
                }
            }
            else if (korisnikID == null)
            {
                return Challenge();
            }

            // Ukloni postojeće sportove korisnika
            var postojeciSportovi = await _context.KorisnickiSportovi
                .Where(ks => ks.KorisnikID == korisnikID)
                .ToListAsync();

            _context.KorisnickiSportovi.RemoveRange(postojeciSportovi);

            // Dodaj odabrane sportove
            foreach (var sportId in selectedSports)
            {
                _context.KorisnickiSportovi.Add(new KorisnikSport
                {
                    KorisnikID = korisnikID.Value,
                    SportID = sportId
                });
            }

            await _context.SaveChangesAsync();

            // Preusmjeri na odgovarajuću stranicu nakon uspješne operacije
            if (Request.Query.ContainsKey("returnUrl"))
            {
                return Redirect(Request.Query["returnUrl"]);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: Sport/List
        public async Task<IActionResult> List()
        {
            // Dohvati sve sportove
            var sportovi = await _context.Sportovi.ToListAsync();
            return View(sportovi);
        }
    }
}
