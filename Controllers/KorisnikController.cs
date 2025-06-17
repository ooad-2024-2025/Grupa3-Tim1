using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Matchletic.Data;
using Matchletic.Models;
using Microsoft.AspNetCore.Authorization;

namespace Matchletic.Controllers
{
    public class KorisnikController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KorisnikController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Korisnik
        [Authorize]
        public async Task<IActionResult> Index(string searchTerm = null)
        {
            // Dohvati trenutno prijavljenog korisnika
            var currentUserId = HttpContext.Session.GetInt32("KorisnikID");

            // Dohvati sve korisnike osim trenutno prijavljenog
            var query = _context.Korisnici
                .Include(k => k.KorisnickiSportovi)
                    .ThenInclude(ks => ks.Sport)
                .Include(k => k.MeceviKorisnika)
                .Where(k => k.KorisnikID != currentUserId);

            // Ako postoji searchTerm, filtriraj rezultate
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(k => 
                    k.Ime.ToLower().Contains(searchTerm) || 
                    k.Prezime.ToLower().Contains(searchTerm) ||
                    k.KorisnickiSportovi.Any(ks => ks.Sport.Naziv.ToLower().Contains(searchTerm))
                );
            }

            var korisnici = await query.ToListAsync();
            ViewBag.SearchTerm = searchTerm;
            return View(korisnici);
        }

        // GET: Korisnik/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var korisnik = await _context.Korisnici
                .FirstOrDefaultAsync(m => m.KorisnikID == id);
            if (korisnik == null)
            {
                return NotFound();
            }

            return View(korisnik);
        }

        // GET: Korisnik/Create
        public IActionResult Create()
        {
            // Add dropdown options for Uloga (roles)
            ViewBag.UlogaOptions = Enum.GetValues(typeof(Uloga))
                .Cast<Uloga>()
                .Select(u => new SelectListItem
                {
                    Text = u.ToString(),
                    Value = ((int)u).ToString()
                });

            // Set default value for Aktivan
            var korisnik = new Korisnik { Aktivan = true };
            return View(korisnik);
        }

        // POST: Korisnik/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("KorisnikID,Lozinka,Ime,Prezime,Email,Uloga,Aktivan")] Korisnik korisnik)
        {
            // Ispiši greške validacije za debugging
            foreach (var modelStateEntry in ModelState.Values)
            {
                foreach (var error in modelStateEntry.Errors)
                {
                    Console.WriteLine($"Validation error: {error.ErrorMessage}");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Inicijaliziraj kolekcije da se izbjegne problem s null referencama
                    korisnik.MeceviKorisnika = new List<MecKorisnik>();
                    korisnik.NapisaneRecenzije = new List<Recenzija>();

                    // Dodaj u bazu
                    _context.Add(korisnik);
                    await _context.SaveChangesAsync();

                    // Preusmjeri na indeks
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Dodaj grešku u ModelState ako dođe do iznimke
                    ModelState.AddModelError("", "Greška pri dodavanju korisnika: " + ex.Message);
                    // Ispiši detalje greške
                    Console.WriteLine($"Exception: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }

            // Ako validacija ne uspije, ponovno postavi padajući izbornik
            ViewBag.UlogaOptions = Enum.GetValues(typeof(Uloga))
                .Cast<Uloga>()
                .Select(u => new SelectListItem
                {
                    Text = u.ToString(),
                    Value = ((int)u).ToString()
                });

            // Vrati na stranicu za stvaranje
            return View(korisnik);
        }

        // GET: Korisnik/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var korisnik = await _context.Korisnici.FindAsync(id);
            if (korisnik == null)
            {
                return NotFound();
            }
            // Add dropdown options for Uloga (roles)
            ViewBag.UlogaOptions = Enum.GetValues(typeof(Uloga))
                .Cast<Uloga>()
                .Select(u => new SelectListItem
                {
                    Text = u.ToString(),
                    Value = ((int)u).ToString()
                });

            return View(korisnik);
        }

        // POST: Korisnik/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("KorisnikID,Lozinka,Ime,Prezime,Email,Uloga,Aktivan")] Korisnik korisnik)
        {
            if (id != korisnik.KorisnikID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(korisnik);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KorisnikExists(korisnik.KorisnikID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(korisnik);
        }

        // GET: Korisnik/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var korisnik = await _context.Korisnici
                .FirstOrDefaultAsync(m => m.KorisnikID == id);
            if (korisnik == null)
            {
                return NotFound();
            }

            return View(korisnik);
        }

        // POST: Korisnik/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var korisnik = await _context.Korisnici.FindAsync(id);
            if (korisnik != null)
            {
                _context.Korisnici.Remove(korisnik);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool KorisnikExists(int id)
        {
            return _context.Korisnici.Any(e => e.KorisnikID == id);
        }

        // GET: Korisnik/Prijava
        public IActionResult Prijava()
        {
            return View();
        }

        // POST: Korisnik/Prijava
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Prijava(Korisnik model)
        {
            if (ModelState.IsValid)
            {
                var korisnik = await _context.Korisnici
                    .FirstOrDefaultAsync(k => k.Email == model.Email && k.Lozinka == model.Lozinka);

                if (korisnik != null)
                {
                    // Ovdje bi trebala biti implementacija prijave (npr. korištenjem Cookiesa)
                    // Za jednostavnu implementaciju, možete koristiti Session
                    HttpContext.Session.SetInt32("KorisnikID", korisnik.KorisnikID);
                    HttpContext.Session.SetString("KorisnikIme", korisnik.Ime);

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Neispravna email adresa ili lozinka");
            }

            return View(model);
        }

        // GET: Korisnik/Registracija
        public IActionResult Registracija()
        {
            return View();
        }

        // POST: Korisnik/Registracija
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registracija(Korisnik model, string potvrdiLozinku)
        {
            if (model.Lozinka != potvrdiLozinku)
            {
                ModelState.AddModelError("", "Lozinka i potvrda lozinke se ne podudaraju.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                // Postavi zadane vrijednosti
                model.Aktivan = true;
                model.Ocjena = 0;
                model.Uloga = Uloga.Korisnik;
                model.JeAdmin = false;

                // Inicijaliziraj kolekcije
                model.MeceviKorisnika = new List<MecKorisnik>();
                model.NapisaneRecenzije = new List<Recenzija>();
                model.KorisnickiSportovi = new List<KorisnikSport>();

                // Provjeri postoji li već korisnik s istim emailom
                if (await _context.Korisnici.AnyAsync(k => k.Email == model.Email))
                {
                    ModelState.AddModelError("", "Email već postoji u sustavu.");
                    return View(model);
                }

                try
                {
                    _context.Add(model);
                    await _context.SaveChangesAsync();

                    // Automatski prijavi novog korisnika
                    HttpContext.Session.SetInt32("KorisnikID", model.KorisnikID);
                    HttpContext.Session.SetString("KorisnikIme", model.Ime);

                    // Preusmjeri na odabir sportova
                    return RedirectToAction("SportSelector", "Onboarding");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Došlo je do greške: " + ex.Message);
                }
            }

            return View(model);
        }

        // GET: Korisnik/GetChallengeModal/5
        [Authorize]
        public async Task<IActionResult> GetChallengeModal(int korisnikId)
        {
            var currentUserId = HttpContext.Session.GetInt32("KorisnikID");
            if (currentUserId == null)
            {
                return Challenge();
            }

            var korisnik = await _context.Korisnici
                .Include(k => k.KorisnickiSportovi)
                    .ThenInclude(ks => ks.Sport)
                .FirstOrDefaultAsync(k => k.KorisnikID == korisnikId);

            if (korisnik == null)
            {
                return NotFound();
            }

            // Dohvati sportove za padajući izbornik
            ViewBag.Sportovi = await _context.Sportovi.ToListAsync();

            // Parcijalni pogled za modal
            return PartialView("_ChallengeModal", korisnik);
        }

    }
}