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

            // Dohvati statistiku korisnika
            var zavrseniMecevi = await _context.MeceviKorisnici
                .Where(mk => mk.KorisnikID == korisnikID)
                .Include(mk => mk.Mec)
                    .ThenInclude(m => m.Sport)
                .Where(mk => mk.Mec.Status == StatusMeca.Zavrsen)
                .OrderByDescending(mk => mk.Mec.DatumMeca)
                .Select(mk => mk.Mec)
                .ToListAsync();

            var kreiraniMecevi = _context.Mecevi
                .Count(m => m.KreatorID == korisnikID);

            // Calculate win rate (you'd need to implement logic to determine wins)
            // For this example, we'll use a placeholder value
            // Izračunaj stvarnu stopu pobjeda na temelju rezultata završenih mečeva
            int ukupnoMeceva = zavrseniMecevi.Count;
            int ukupnoPobjeda = 0;
            int winRate;

            if (ukupnoMeceva > 0)
            {
                // Pretpostavljamo da je korisnik pobjednik ako je njegov tim prvi u rezultatu
                // npr. "2-1" za formata "TIM1-TIM2"
                foreach (var mec in zavrseniMecevi)
                {
                    if (!string.IsNullOrEmpty(mec.Rezultat) && mec.Rezultat.Contains("-"))
                    {
                        var rezultati = mec.Rezultat.Split('-');
                        if (rezultati.Length == 2)
                        {
                            int rezultatTim1, rezultatTim2;
                            if (int.TryParse(rezultati[0], out rezultatTim1) &&
                                int.TryParse(rezultati[1], out rezultatTim2))
                            {
                                // Provjeri je li korisnik kreator (Tim 1) ili gost (Tim 2)
                                bool jeKreator = mec.KreatorID == korisnikID;

                                // Pobjeda ako je korisnik kreator i Tim 1 ima veći rezultat
                                // ili ako korisnik nije kreator i Tim 2 ima veći rezultat
                                if ((jeKreator && rezultatTim1 > rezultatTim2) ||
                                    (!jeKreator && rezultatTim2 > rezultatTim1))
                                {
                                    ukupnoPobjeda++;
                                }
                            }
                        }
                    }
                }

                // Izračunaj postotak pobjeda
                winRate = (int)((double)ukupnoPobjeda / ukupnoMeceva * 100);
            }
            else
            {
                // Ako nema završenih mečeva, stopa pobjeda je 0
                winRate = 0;
            }
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

            ViewBag.ZavrseniMecevi = zavrseniMecevi;
            ViewBag.KreiraniMecevi = kreiraniMecevi;
            ViewBag.MatchesPlayed = zavrseniMecevi;
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
        public async Task<IActionResult> Edit(int id, [Bind("KorisnikID,Ime,Prezime,Email,Lokacija")] Korisnik korisnik, IFormFile profilnaSlika, int[] odabraniSportovi)
        {
            // Osnovna validacija
            if (id != korisnik.KorisnikID)
            {
                return NotFound();
            }

            // Provjera trenutnog korisnika
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null || korisnikID != id)
            {
                return Challenge();
            }

            // Ukloni validacijske greške za kolekcije
            ModelState.Remove("KorisnickiSportovi");
            ModelState.Remove("MeceviKorisnika");
            ModelState.Remove("NapisaneRecenzije");
            ModelState.Remove("Lozinka");
            ModelState.Remove("Uloga");
            ModelState.Remove("Aktivan");
            ModelState.Remove("JeAdmin");
            ModelState.Remove("Ocjena");
            ModelState.Remove("ProfilnaSlika");

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Dohvati postojećeg korisnika
                    var postojeciKorisnik = await _context.Korisnici
                        .Include(k => k.KorisnickiSportovi)
                        .FirstOrDefaultAsync(k => k.KorisnikID == id);

                    if (postojeciKorisnik == null)
                    {
                        return NotFound();
                    }

                    // 2. Postavi profilnu sliku ako je učitana
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

                        postojeciKorisnik.ProfilnaSlika = "/images/profil/" + fileName;
                    }

                    // 3. Ažuriraj osnovne podatke
                    postojeciKorisnik.Ime = korisnik.Ime;
                    postojeciKorisnik.Prezime = korisnik.Prezime;
                    postojeciKorisnik.Email = korisnik.Email;
                    postojeciKorisnik.Lokacija = korisnik.Lokacija;

                    // 4. Spremi promjene za korisnika
                    _context.Update(postojeciKorisnik);
                    await _context.SaveChangesAsync();

                    // 5. Obriši postojeće sportove
                    var trenutniSportovi = await _context.KorisnickiSportovi
                        .Where(ks => ks.KorisnikID == id)
                        .ToListAsync();

                    if (trenutniSportovi.Any())
                    {
                        _context.KorisnickiSportovi.RemoveRange(trenutniSportovi);
                        await _context.SaveChangesAsync();
                    }

                    // 6. Dodaj nove sportove
                    if (odabraniSportovi != null && odabraniSportovi.Length > 0)
                    {
                        foreach (var sportId in odabraniSportovi)
                        {
                            _context.KorisnickiSportovi.Add(new KorisnikSport
                            {
                                KorisnikID = id,
                                SportID = sportId
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    // 7. Ažuriraj identity korisnika ako je promijenjen email
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

                    // 8. Ažuriraj sesiju
                    HttpContext.Session.SetString("KorisnikIme", postojeciKorisnik.Ime);

                    TempData["SuccessMessage"] = "Vaš profil je uspješno ažuriran!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Greška pri ažuriranju profila: {ex.Message}");
                }
            }

            // Ako dođemo do ovdje, nešto je pošlo po zlu - prikaži formu ponovno
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
