using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Matchletic.Data;
using Matchletic.Models;

namespace Matchletic.Controllers
{
    public class OnboardingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OnboardingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Onboarding
        public IActionResult Index()
        {
            return View();
        }

        // GET: Onboarding/Step1
        public IActionResult Step1()
        {
            // Provjerite je li korisnik već prijavljen
            var userId = HttpContext.Session.GetInt32("KorisnikID");
            if (userId != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Onboarding/Step1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step1(Korisnik korisnik)
        {
            if (ModelState.IsValid)
            {
                // Postavi osnovne vrijednosti
                korisnik.Uloga = Uloga.Korisnik;
                korisnik.Aktivan = true;
                korisnik.JeAdmin = false;
                korisnik.Ocjena = 0;
                korisnik.KorisnickiSportovi = new List<KorisnikSport>();
                korisnik.MeceviKorisnika = new List<MecKorisnik>();
                korisnik.NapisaneRecenzije = new List<Recenzija>();

                // Provjera postoji li već korisnik s istim emailom
                var postojeciKorisnik = await _context.Korisnici
                    .FirstOrDefaultAsync(k => k.Email == korisnik.Email);

                if (postojeciKorisnik != null)
                {
                    ModelState.AddModelError("Email", "Korisnik s ovom email adresom već postoji.");
                    return View(korisnik);
                }

                _context.Add(korisnik);
                await _context.SaveChangesAsync();

                // Spremi korisnika u sesiju
                HttpContext.Session.SetInt32("KorisnikID", korisnik.KorisnikID);
                HttpContext.Session.SetString("KorisnikIme", korisnik.Ime);

                return RedirectToAction(nameof(Step2));
            }
            return View(korisnik);
        }

        // GET: Onboarding/Step2
        public async Task<IActionResult> Step2()
        {
            // Provjeri je li korisnik prošao kroz Step1
            var userId = HttpContext.Session.GetInt32("KorisnikID");
            if (userId == null)
            {
                return RedirectToAction(nameof(Step1));
            }

            // Dohvati sve sportove
            var sportovi = await _context.Sportovi.ToListAsync();
            return View(sportovi);
        }

        // POST: Onboarding/Step2
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step2(List<int> odabraniSportovi)
        {
            var userId = HttpContext.Session.GetInt32("KorisnikID");
            if (userId == null)
            {
                return RedirectToAction(nameof(Step1));
            }

            if (odabraniSportovi != null && odabraniSportovi.Any())
            {
                // Dohvati korisnika
                var korisnik = await _context.Korisnici
                    .Include(k => k.KorisnickiSportovi)
                    .FirstOrDefaultAsync(k => k.KorisnikID == userId);

                if (korisnik == null)
                {
                    return RedirectToAction(nameof(Step1));
                }

                // Dodaj odabrane sportove
                foreach (var sportId in odabraniSportovi)
                {
                    // Provjeri postoji li već taj sport za korisnika
                    if (!korisnik.KorisnickiSportovi.Any(ks => ks.SportID == sportId))
                    {
                        korisnik.KorisnickiSportovi.Add(new KorisnikSport
                        {
                            KorisnikID = korisnik.KorisnikID,
                            SportID = sportId
                        });
                    }
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
