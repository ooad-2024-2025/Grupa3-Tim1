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

            // Ako je AJAX zahtjev, vrati OK status
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok();
            }

            // Inače, preusmjeri korisnika
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

        [HttpGet]
        public async Task<IActionResult> TestIzazovNotifikacija(int targetUserId)
        {
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            try
            {
                // Dohvati osnovne podatke
                var kreator = await _context.Korisnici.FindAsync(korisnikID);
                if (kreator == null)
                {
                    TempData["ErrorMessage"] = "Kreator nije pronađen.";
                    return RedirectToAction("Index");
                }

                // Provjeri postoji li ciljani korisnik
                var ciljaniKorisnik = await _context.Korisnici.FindAsync(targetUserId);
                if (ciljaniKorisnik == null)
                {
                    TempData["ErrorMessage"] = "Ciljani korisnik nije pronađen.";
                    return RedirectToAction("Index");
                }

                // Stvori testni meč
                var testMec = new Mec
                {
                    Naslov = "Test meč za izazov",
                    Opis = "Ovo je test meč za dijagnostiku notifikacija izazova",
                    KreatorID = korisnikID.Value,
                    DatumKreiranja = DateTime.Now,
                    DatumMeca = DateTime.Now.AddDays(1),
                    Lokacija = "Test lokacija",
                    BrojIgraca = 2,
                    SportID = 1, // Pretpostavljamo da sport s ID 1 postoji
                    Status = StatusMeca.CekaPrihvacanje,
                    JePrivatan = true
                };

                _context.Mecevi.Add(testMec);
                await _context.SaveChangesAsync();

                // Dodaj korisnike u meč
                _context.MeceviKorisnici.Add(new MecKorisnik
                {
                    KorisnikID = korisnikID.Value,
                    MecID = testMec.MecID,
                    DatumMeca = DateTime.Now,
                    JePrihvacen = true
                });

                _context.MeceviKorisnici.Add(new MecKorisnik
                {
                    KorisnikID = targetUserId,
                    MecID = testMec.MecID,
                    DatumMeca = DateTime.Now,
                    JePrihvacen = false
                });

                await _context.SaveChangesAsync();

                // Kreiraj notifikaciju direktno
                var notifikacija = new Notifikacija
                {
                    KorisnikID = targetUserId,
                    Naslov = "Test izazov",
                    Sadrzaj = $"{kreator.Ime} {kreator.Prezime} te izazvao na test meč.",
                    Url = $"/Mec/Details/{testMec.MecID}",
                    Tip = NotifikacijaTip.Izazov,
                    DatumKreiranja = DateTime.Now,
                    Procitano = false
                };

                _context.Notifikacije.Add(notifikacija);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Test izazov s notifikacijom je uspješno kreiran!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Greška prilikom kreiranja test izazova: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $" ({ex.InnerException.Message})";
                }
                return RedirectToAction("Index");
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetNeprocitane()
        {
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Json(new { count = 0 });
            }

            var count = await _notifikacijaService.GetBrojNeprocitanihNotifikacijaAsync(korisnikID.Value);
            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> Dijagnostika()
        {
            if (!User.IsInRole("Admin")) // Provjera da li je korisnik admin, ako imate takvu ulogu
            {
                var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
                if (korisnikID == null)
                {
                    return Challenge();
                }
            }

            try
            {
                var rezultati = new Dictionary<string, object>();

                // Dohvati sve notifikacije
                var sveNotifikacije = await _context.Notifikacije
                    .OrderByDescending(n => n.DatumKreiranja)
                    .Take(50)
                    .ToListAsync();

                // Grupiraj po tipu
                var grupirano = sveNotifikacije
                    .GroupBy(n => n.Tip)
                    .Select(g => new { Tip = g.Key, Broj = g.Count() })
                    .ToList();

                rezultati.Add("UkupnoNotifikacija", sveNotifikacije.Count);
                rezultati.Add("GrupeNotifikacija", grupirano);
                rezultati.Add("ZadnjihPet", sveNotifikacije.Take(5));

                // Dohvati notifikacije vezane za izazove
                var izazovNotifikacije = await _context.Notifikacije
                    .Where(n => n.Tip == NotifikacijaTip.Izazov)
                    .OrderByDescending(n => n.DatumKreiranja)
                    .Take(10)
                    .ToListAsync();

                rezultati.Add("IzazovNotifikacije", izazovNotifikacije);

                return View(rezultati);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Greška prilikom dijagnostike: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestServis(int targetUserId)
        {
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            try
            {
                // Kreirajmo testni meč kao u prethodnoj metodi
                var testMec = new Mec
                {
                    Naslov = "Test meč za servis",
                    Opis = "Ovo je test meč za testiranje NotifikacijaService",
                    KreatorID = korisnikID.Value,
                    DatumKreiranja = DateTime.Now,
                    DatumMeca = DateTime.Now.AddDays(1),
                    Lokacija = "Test lokacija",
                    BrojIgraca = 2,
                    SportID = 1,
                    Status = StatusMeca.CekaPrihvacanje,
                    JePrivatan = true
                };

                _context.Mecevi.Add(testMec);
                await _context.SaveChangesAsync();

                // Dodaj korisnike u meč
                _context.MeceviKorisnici.Add(new MecKorisnik
                {
                    KorisnikID = korisnikID.Value,
                    MecID = testMec.MecID,
                    DatumMeca = DateTime.Now,
                    JePrihvacen = true
                });

                _context.MeceviKorisnici.Add(new MecKorisnik
                {
                    KorisnikID = targetUserId,
                    MecID = testMec.MecID,
                    DatumMeca = DateTime.Now,
                    JePrihvacen = false
                });

                await _context.SaveChangesAsync();

                // Pozovi servis direktno
                var result = await _notifikacijaService.KreirajNotifikacijuIzazovaAsync(
                    targetUserId,
                    testMec.MecID,
                    korisnikID.Value);

                if (result)
                {
                    TempData["SuccessMessage"] = "NotifikacijaService.KreirajNotifikacijuIzazovaAsync vratio TRUE - notifikacija bi trebala biti kreirana.";
                }
                else
                {
                    TempData["ErrorMessage"] = "NotifikacijaService.KreirajNotifikacijuIzazovaAsync vratio FALSE - notifikacija NIJE kreirana.";
                }

                return RedirectToAction("Dijagnostika");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Greška prilikom testiranja servisa: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $" ({ex.InnerException.Message})";
                }
                return RedirectToAction("Index");
            }
        }

        // Testna metoda za provjeru rada notifikacija
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> TestNotifikacija()
        {
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            try
            {
                var notifikacija = new Notifikacija
                {
                    KorisnikID = korisnikID.Value,
                    Naslov = "Test notifikacija",
                    Sadrzaj = "Ovo je testna notifikacija za provjeru sustava.",
                    Url = "/Notifikacija/Index",
                    Tip = NotifikacijaTip.Izazov,
                    DatumKreiranja = DateTime.Now,
                    Procitano = false
                };

                _context.Notifikacije.Add(notifikacija);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Testna notifikacija je uspješno kreirana!";
                return RedirectToAction("Dijagnostika");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Greška prilikom kreiranja testne notifikacije: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $" ({ex.InnerException.Message})";
                }
                return RedirectToAction("Dijagnostika");
            }
        }


    }
}
