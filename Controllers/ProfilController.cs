// Controllers/ProfilController.cs
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Matchletic.Data;
using Matchletic.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Matchletic.Services;

namespace Matchletic.Controllers
{
    [Authorize]
    public class ProfilController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserSyncService _userSyncService;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfilController(
            ApplicationDbContext context,
            UserSyncService userSyncService,
            IWebHostEnvironment hostEnvironment,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userSyncService = userSyncService;
            _hostEnvironment = hostEnvironment;
            _userManager = userManager;
        }

        // GET: Profil
        public async Task<IActionResult> Index()
        {
            // Get current user ID
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");

            // If session is not set but user is authenticated, try to sync and set it
            if (korisnikID == null && User.Identity.IsAuthenticated)
            {
                var email = User.Identity.Name;
                Console.WriteLine($"Profil/Index: User is authenticated with email {email}, but no KorisnikID in session");

                // Sync the user
                await _userSyncService.SyncIdentityUser(email);

                // Try to get the korisnikID again from the database
                var korisnik = await _context.Korisnici.FirstOrDefaultAsync(k => k.Email == email);
                if (korisnik != null)
                {
                    korisnikID = korisnik.KorisnikID;
                    HttpContext.Session.SetInt32("KorisnikID", korisnikID.Value);
                    HttpContext.Session.SetString("KorisnikIme", korisnik.Ime);
                    Console.WriteLine($"Profil/Index: Set KorisnikID in session to {korisnikID}");
                }
                else
                {
                    Console.WriteLine("Profil/Index: Failed to find or create Korisnik");
                    return Challenge();
                }
            }
            else if (korisnikID == null)
            {
                Console.WriteLine("Profil/Index: User is not authenticated and no KorisnikID in session");
                return Challenge();
            }

            var korisnikProfil = await _context.Korisnici
                .Include(k => k.KorisnickiSportovi)
                .ThenInclude(ks => ks.Sport)
                .Include(k => k.MeceviKorisnika)
                .ThenInclude(mk => mk.Mec)
                .FirstOrDefaultAsync(k => k.KorisnikID == korisnikID);

            if (korisnikProfil == null)
            {
                return NotFound();
            }

            // Izračunaj broj pobjeda i poraza koristeći MecConfirmation
            var ukupnoPobjeda = await _context.MecConfirmation.CountAsync(mc => mc.KorisnikID == korisnikID && mc.IsWinner);
            var ukupnoPoraza = await _context.MecConfirmation.CountAsync(mc => mc.KorisnikID == korisnikID && !mc.IsWinner);
            var ukupnoMeceva = ukupnoPobjeda + ukupnoPoraza;
            int winRate = ukupnoMeceva > 0 ? (int)((double)ukupnoPobjeda / ukupnoMeceva * 100) : 0;

            // User rank from leaderboard (placeholder)
            var rank = "1,234";

            // Get user's match history by sport
            var sportMatches = await _context.MeceviKorisnici
                .Where(mk => mk.KorisnikID == korisnikID)
                .Include(mk => mk.Mec)
                .ThenInclude(m => m.Sport)
                .GroupBy(mk => mk.Mec.Sport.Naziv)
                .Select(g => new { Sport = g.Key, Count = g.Count() })
                .ToListAsync();

            // Calculate percentages for each sport
            var totalMatches = sportMatches.Sum(sm => sm.Count);
            var sportPercentages = sportMatches
                .Select(sm => new {
                    Sport = sm.Sport,
                    Count = sm.Count,
                    Percentage = totalMatches > 0 ? (int)(sm.Count * 100.0 / totalMatches) : 0
                })
                .ToList();

            // Get monthly activity
            var monthlyActivity = await _context.MeceviKorisnici
                .Where(mk => mk.KorisnikID == korisnikID && mk.DatumMeca >= DateTime.Now.AddMonths(-6))
                .GroupBy(mk => new { Month = mk.DatumMeca.Month, Year = mk.DatumMeca.Year })
                .Select(g => new {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Count = g.Count()
                })
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToListAsync();

            // Prepare month labels and values
            var months = new List<string>();
            var values = new List<int>();

            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.Now.AddMonths(-i);
                var monthName = date.ToString("MMM").Substring(0, 3);
                months.Add(monthName);

                var activity = monthlyActivity.FirstOrDefault(ma => ma.Month == date.Month && ma.Year == date.Year);
                values.Add(activity?.Count ?? 0);
            }

            ViewBag.ZavrseniMecevi = korisnikProfil.MeceviKorisnika.Select(mk => mk.Mec).ToList();
            ViewBag.KreiraniMecevi = _context.Mecevi.Count(m => m.KreatorID == korisnikID);
            ViewBag.MatchesPlayed = korisnikProfil.MeceviKorisnika.Select(mk => mk.Mec).ToList();
            ViewBag.WinRate = winRate;
            ViewBag.Rating = korisnikProfil.Ocjena;
            ViewBag.Rank = rank;
            ViewBag.SportPercentages = sportPercentages;
            ViewBag.Months = months;
            ViewBag.Values = values;
            ViewBag.SportOptions = await _context.Sportovi.ToListAsync();
            ViewBag.CurrentUserId = korisnikID;

            return View(korisnikProfil);
        }

        // GET: Profil/Edit
        public async Task<IActionResult> Edit()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Challenge();
            }

            var email = User.Identity.Name;

            // Uvijek dohvati korisnika po trenutnom emailu
            var korisnik = await _context.Korisnici
                .Include(k => k.KorisnickiSportovi)
                .ThenInclude(ks => ks.Sport)
                .FirstOrDefaultAsync(k => k.Email == email);

            if (korisnik == null)
            {
                // Ako ne postoji, pokušaj sinkronizirati
                await _userSyncService.SyncIdentityUser(email);
                korisnik = await _context.Korisnici.FirstOrDefaultAsync(k => k.Email == email);

                if (korisnik == null)
                {
                    return NotFound();
                }
            }

            // Osvježi sesiju s pravim KorisnikID
            HttpContext.Session.SetInt32("KorisnikID", korisnik.KorisnikID);
            HttpContext.Session.SetString("KorisnikIme", korisnik.Ime);

            ViewBag.Sportovi = new MultiSelectList(_context.Sportovi, "SportID", "Naziv",
                korisnik.KorisnickiSportovi?.Select(ks => ks.SportID));

            return View(korisnik);
        }

        // POST: Profil/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("KorisnikID,Ime,Prezime,Email,ProfilnaSlika")] Korisnik korisnik, IFormFile profilnaSlika, int[] odabraniSportovi)
        {
            if (id != korisnik.KorisnikID)
            {
                return NotFound();
            }

            // Get current user ID
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null || korisnikID != id)
            {
                return Challenge();
            }

            // Remove validation errors for collections
            ModelState.Remove("KorisnickiSportovi");
            ModelState.Remove("MeceviKorisnika");
            ModelState.Remove("NapisaneRecenzije");
            ModelState.Remove("Lozinka");
            ModelState.Remove("Uloga");
            ModelState.Remove("Aktivan");
            ModelState.Remove("JeAdmin");
            ModelState.Remove("Ocjena");

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing user with collections
                    var postojeciKorisnik = await _context.Korisnici
                        .Include(k => k.KorisnickiSportovi)
                        .FirstOrDefaultAsync(k => k.KorisnikID == id);

                    if (postojeciKorisnik == null)
                    {
                        return NotFound();
                    }

                    // Upload profilne slike ako je postavljena
                    if (profilnaSlika != null && profilnaSlika.Length > 0)
                    {
                        var uploadDir = Path.Combine(_hostEnvironment.WebRootPath, "images/profil");
                        if (!Directory.Exists(uploadDir))
                        {
                            Directory.CreateDirectory(uploadDir);
                        }

                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profilnaSlika.FileName);
                        var filePath = Path.Combine(uploadDir, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await profilnaSlika.CopyToAsync(stream);
                        }

                        // Set profile image path
                        postojeciKorisnik.ProfilnaSlika = "/images/profil/" + fileName;
                    }

                    // Update basic info
                    postojeciKorisnik.Ime = korisnik.Ime;
                    postojeciKorisnik.Prezime = korisnik.Prezime;
                    postojeciKorisnik.Email = korisnik.Email;

                    // Update sports
                    if (odabraniSportovi != null && odabraniSportovi.Length > 0)
                    {
                        // Remove existing sports
                        _context.KorisnickiSportovi.RemoveRange(postojeciKorisnik.KorisnickiSportovi);

                        // Add selected sports
                        foreach (var sportId in odabraniSportovi)
                        {
                            postojeciKorisnik.KorisnickiSportovi.Add(new KorisnikSport
                            {
                                KorisnikID = postojeciKorisnik.KorisnikID,
                                SportID = sportId
                            });
                        }
                    }

                    _context.Update(postojeciKorisnik);
                    await _context.SaveChangesAsync();

                    // Update Identity user email if changed
                    if (User.Identity.Name != korisnik.Email)
                    {
                        var identityUser = await _userManager.FindByEmailAsync(User.Identity.Name);
                        if (identityUser != null)
                        {
                            identityUser.Email = korisnik.Email;
                            identityUser.UserName = korisnik.Email;
                            await _userManager.UpdateAsync(identityUser);
                        }
                    }

                    // Update session
                    HttpContext.Session.SetString("KorisnikIme", postojeciKorisnik.Ime);

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Korisnici.Any(k => k.KorisnikID == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Sportovi = new MultiSelectList(_context.Sportovi, "SportID", "Naziv", odabraniSportovi);
            return View(korisnik);
        }

        // GET: Profil/Sportovi
        // GET: Profil/Sportovi
        public async Task<IActionResult> Sportovi()
        {
            // Get current user ID
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");

            // Provjere i sinhronizacija
            if (korisnikID == null && User.Identity.IsAuthenticated)
            {
                var email = User.Identity.Name;
                await _userSyncService.SyncIdentityUser(email);
                var korisnik = await _context.Korisnici.FirstOrDefaultAsync(k => k.Email == email);
                if (korisnik != null)
                {
                    korisnikID = korisnik.KorisnikID;
                    HttpContext.Session.SetInt32("KorisnikID", korisnikID.Value);
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

            // Dohvati korisnikove sportove s eager loadingom za Sport
            var korisnikSportovi = await _context.KorisnickiSportovi
                .Include(ks => ks.Sport)
                .Where(ks => ks.KorisnikID == korisnikID)
                .ToListAsync();

            // Dohvati ID-eve sportova koje korisnik već ima
            var korisnikSportIds = korisnikSportovi.Select(ks => ks.SportID).ToList();

            // Dohvati dostupne sportove (one koje korisnik nema)
            var dostupniSportovi = await _context.Sportovi
                .Where(s => !korisnikSportIds.Contains(s.SportID))
                .ToListAsync();

            ViewBag.DostupniSportovi = dostupniSportovi;
            ViewBag.CurrentUserId = korisnikID;

            return View(korisnikSportovi);
        }


        // POST: Profil/DodajSport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DodajSport(int sportId)
        {
            // Get current user ID
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            // Provjeri da sport postoji
            var sport = await _context.Sportovi.FindAsync(sportId);
            if (sport == null)
            {
                return NotFound();
            }

            // Provjeri da korisnik već nema taj sport - POBOLJŠANI UPIT
            var postojeciSport = await _context.KorisnickiSportovi
                .AsNoTracking()
                .AnyAsync(ks => ks.KorisnikID == korisnikID && ks.SportID == sportId);

            if (!postojeciSport)
            {
                // Dodaj sport korisniku
                _context.KorisnickiSportovi.Add(new KorisnikSport
                {
                    KorisnikID = korisnikID.Value,
                    SportID = sportId
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Sportovi));
        }


        // POST: Profil/UkloniSport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UkloniSport(int sportId)
        {
            // Get current user ID
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            // Pronađi sport korisnika
            var korisnikSport = await _context.KorisnickiSportovi
                .FirstOrDefaultAsync(ks => ks.KorisnikID == korisnikID && ks.SportID == sportId);

            if (korisnikSport != null)
            {
                _context.KorisnickiSportovi.Remove(korisnikSport);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Sportovi));
        }
    }
}
