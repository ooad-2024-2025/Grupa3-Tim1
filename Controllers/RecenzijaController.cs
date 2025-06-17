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
    public class RecenzijaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecenzijaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Recenzija
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Recenzije.Include(r => r.Autor).Include(r => r.Mec);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Recenzija/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzija = await _context.Recenzije
                .Include(r => r.Autor)
                .Include(r => r.Mec)
                .FirstOrDefaultAsync(m => m.RecenzijaID == id);
            if (recenzija == null)
            {
                return NotFound();
            }

            return View(recenzija);
        }

        // GET: Recenzija/Create
        [Authorize]
        public IActionResult Create(int? mecId = null)
        {
            // Get current user ID
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            // Dohvati mečeve s naslovom gdje je korisnik sudionik i meč je završen
            var mecevi = _context.Mecevi
                .Where(m => m.Status == StatusMeca.Zavrsen && 
                           m.KorisniciMeca.Any(km => km.KorisnikID == korisnikID) &&
                           !m.Recenzije.Any(r => r.AutorID == korisnikID))
                .Select(m => new
                {
                    m.MecID,
                    DisplayText = $"{m.Naslov} ({m.DatumMeca.ToString("dd.MM.yyyy")})"
                })
                .ToList();

            // Kreiraj SelectList s informativnijim tekstom
            ViewBag.MecOptions = new SelectList(mecevi, "MecID", "DisplayText", mecId);

            // Create new review with pre-filled values
            var recenzija = new Recenzija
            {
                AutorID = korisnikID.Value,
                MecID = mecId ?? 0
            };

            return View(recenzija);
        }

        // POST: Recenzija/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("MecID,Ocjena,Komentar")] Recenzija recenzija)
        {
            // Get current user ID
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            // Set the author to the current user
            recenzija.AutorID = korisnikID.Value;

            // Validate that the user is a participant in the match
            var mec = await _context.Mecevi
                .Include(m => m.KorisniciMeca)
                .FirstOrDefaultAsync(m => m.MecID == recenzija.MecID);

            if (mec == null || mec.Status != StatusMeca.Zavrsen || 
                !mec.KorisniciMeca.Any(km => km.KorisnikID == korisnikID))
            {
                ModelState.AddModelError("", "Ne možete recenzirati ovaj meč.");
                return View(recenzija);
            }

            // Check if user already reviewed this match
            var existingReview = await _context.Recenzije
                .AnyAsync(r => r.MecID == recenzija.MecID && r.AutorID == korisnikID);
            if (existingReview)
            {
                ModelState.AddModelError("", "Već ste recenzirali ovaj meč.");
                return View(recenzija);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(recenzija);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Details", "Mec", new { id = recenzija.MecID });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Greška pri dodavanju recenzije: " + ex.Message);
                }
            }

            // Repopulate the match dropdown if there's an error
            var mecevi = _context.Mecevi
                .Where(m => m.Status == StatusMeca.Zavrsen && 
                           m.KorisniciMeca.Any(km => km.KorisnikID == korisnikID) &&
                           !m.Recenzije.Any(r => r.AutorID == korisnikID))
                .Select(m => new
                {
                    m.MecID,
                    DisplayText = $"{m.Naslov} ({m.DatumMeca.ToString("dd.MM.yyyy")})"
                })
                .ToList();
            ViewBag.MecOptions = new SelectList(mecevi, "MecID", "DisplayText", recenzija.MecID);

            return View(recenzija);
        }

        // GET: Recenzija/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzija = await _context.Recenzije.FindAsync(id);
            if (recenzija == null)
            {
                return NotFound();
            }

            // Dropdown za meč - poboljšano
            var mecevi = _context.Mecevi
                .Select(m => new
                {
                    m.MecID,
                    DisplayText = $"Mec ID: {m.MecID} - {m.Naslov}"
                })
                .ToList();
            ViewBag.MecOptions = new SelectList(mecevi, "MecID", "DisplayText", recenzija.MecID);

            // Dropdown za autora - ispravljeno
            var korisnici = _context.Korisnici
                .Select(k => new
                {
                    k.KorisnikID,
                    PunoIme = $"{k.Ime} {k.Prezime}"
                })
                .ToList();
            ViewBag.AutorOptions = new SelectList(korisnici, "KorisnikID", "PunoIme", recenzija.AutorID);

            return View(recenzija);
        }

        // POST: Recenzija/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RecenzijaID,MecID,Ocjena,Komentar,AutorID")] Recenzija recenzija)
        {
            if (id != recenzija.RecenzijaID)
            {
                return NotFound();
            }

            // Uklonite validacijske greške za navigacijska svojstva
            ModelState.Remove("Mec");
            ModelState.Remove("Autor");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(recenzija);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RecenzijaExists(recenzija.RecenzijaID))
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
            // Ponovno postavljanje dropdown opcija
            var mecevi = _context.Mecevi
                .Select(m => new
                {
                    m.MecID,
                    DisplayText = $"Mec ID: {m.MecID} - {m.Naslov}"
                })
                .ToList();
            ViewBag.MecOptions = new SelectList(mecevi, "MecID", "DisplayText", recenzija.MecID);

            var korisnici = _context.Korisnici
                .Select(k => new
                {
                    k.KorisnikID,
                    PunoIme = $"{k.Ime} {k.Prezime}"
                })
                .ToList();
            ViewBag.AutorOptions = new SelectList(korisnici, "KorisnikID", "PunoIme", recenzija.AutorID);

            return View(recenzija);
        }

        // GET: Recenzija/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzija = await _context.Recenzije
                .Include(r => r.Autor)
                .Include(r => r.Mec)
                .FirstOrDefaultAsync(m => m.RecenzijaID == id);
            if (recenzija == null)
            {
                return NotFound();
            }

            return View(recenzija);
        }

        // POST: Recenzija/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var recenzija = await _context.Recenzije.FindAsync(id);
            if (recenzija != null)
            {
                _context.Recenzije.Remove(recenzija);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RecenzijaExists(int id)
        {
            return _context.Recenzije.Any(e => e.RecenzijaID == id);
        }
    }
}
