using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Matchletic.Data;
using Matchletic.Models;

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
        public IActionResult Create()
        {
            // Dohvati mečeve s naslovom
            var mecevi = _context.Mecevi
                .Select(m => new
                {
                    m.MecID,
                    DisplayText = $"Mec ID: {m.MecID} - {m.Naslov}"
                })
                .ToList();

            // Kreiraj SelectList s informativnijim tekstom
            ViewBag.MecOptions = new SelectList(mecevi, "MecID", "DisplayText");

            // Dropdown za autora s imenom i prezimenom - ispravljeno
            var korisnici = _context.Korisnici
                .Select(k => new
                {
                    k.KorisnikID,
                    PunoIme = $"{k.Ime} {k.Prezime}"
                })
                .ToList();

            ViewBag.AutorOptions = new SelectList(korisnici, "KorisnikID", "PunoIme");

            return View();
        }

        // POST: Recenzija/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RecenzijaID,MecID,Ocjena,Komentar,AutorID")] Recenzija recenzija)
        {
            // Uklonite validacijske greške za navigacijska svojstva
            ModelState.Remove("Mec");
            ModelState.Remove("Autor");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(recenzija);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Greška pri dodavanju recenzije: " + ex.Message);
                    Console.WriteLine($"Exception: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                // Ispišite sve validacijske greške za debugging
                foreach (var modelState in ViewData.ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        Console.WriteLine($"Validation error: {error.ErrorMessage}");
                    }
                }
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
