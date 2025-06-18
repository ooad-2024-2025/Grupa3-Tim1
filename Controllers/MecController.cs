// Controllers/MecController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Matchletic.Data;
using Matchletic.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Matchletic.Services;
using Microsoft.AspNetCore.Identity;


namespace Matchletic.Controllers
{
    public class MecController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserSyncService _userSyncService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly NotifikacijaService _notifikacijaService;
        private readonly MecRequestService _mecRequestService;
        private readonly ILogger<MecController> _logger;


        public MecController(ApplicationDbContext context,
                             UserSyncService userSyncService,
                             UserManager<IdentityUser> userManager,
                             NotifikacijaService notifikacijaService,
                             MecRequestService mecRequestService,
                             ILogger<MecController> logger)

        {
            _context = context;
            _userSyncService = userSyncService;
            _userManager = userManager;
            _notifikacijaService = notifikacijaService;
            _mecRequestService = mecRequestService;
            _logger = logger;
        }

        // GET: Mec
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var email = user.Email;
            var korisnik = _context.Korisnici.FirstOrDefault(k => k.Email == email);
            if (korisnik == null)
            {
                return Challenge();
            }

            var myMatches = _context.Mecevi
                .Where(m => m.KreatorID == korisnik.KorisnikID)
                .ToList();

            return View(myMatches);
        }


        // GET: Mec/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mec = await _context.Mecevi
                .Include(m => m.Sport)
                .Include(m => m.Kreator)
                .Include(m => m.KorisniciMeca)
                .ThenInclude(km => km.Korisnik)
                .FirstOrDefaultAsync(m => m.MecID == id);

            if (mec == null)
            {
                return NotFound();
            }

            ViewBag.CurrentUserId = HttpContext.Session.GetInt32("KorisnikID");

            // Provjeri je li korisnik već poslao zahtjev za ovaj meč
            if (ViewBag.CurrentUserId != null)
            {
                // Pohrani ViewBag vrijednost u lokalnu varijablu
                int currentUserId = ViewBag.CurrentUserId;

                // Koristi lokalnu varijablu umjesto dinamičkog ViewBag objekta
                var postojeciZahtjev = await _context.MecRequests
                    .AnyAsync(r => r.MecID == id && r.KorisnikID == currentUserId && r.Status == MecRequestStatus.Ceka);

                ViewBag.ZahtjevPoslan = postojeciZahtjev;
            }

            return View(mec);
        }



        // GET: Mec/Create
        public IActionResult Create()
        {
            ViewBag.Sports = new SelectList(_context.Sportovi, "SportID", "Naziv");

            var mec = new Mec
            {
                DatumKreiranja = DateTime.Now,
                DatumMeca = DateTime.Now.AddDays(1)
            };

            return View(mec);
        }

        // POST: Mec/Create
        // POST: Mec/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Naslov,Opis,SportID,DatumMeca,Lokacija,BrojIgraca")] Mec mec)
        {
            // Remove validation errors for collections
            ModelState.Remove("KorisniciMeca");
            ModelState.Remove("Recenzije");
            ModelState.Remove("Sport");
            ModelState.Remove("Kreator");

            if (ModelState.IsValid)
            {
                try
                {
                    // Get current user ID
                    var kreatorID = HttpContext.Session.GetInt32("KorisnikID");
                    if (kreatorID == null)
                    {
                        return Challenge();
                    }

                    // Set up new match
                    mec.KreatorID = kreatorID.Value;
                    mec.DatumKreiranja = DateTime.Now;
                    mec.Status = StatusMeca.Objavljen;
                    mec.KorisniciMeca = new List<MecKorisnik>
            {
                new MecKorisnik
                {
                    KorisnikID = kreatorID.Value,
                    DatumMeca = DateTime.Now
                }
            };
                    mec.Recenzije = new List<Recenzija>();

                    // Ensure DatumMeca has a valid time
                    if (mec.DatumMeca.Hour == 0 && mec.DatumMeca.Minute == 0)
                    {
                        // If time is not set, default to noon
                        mec.DatumMeca = mec.DatumMeca.Date.AddHours(12);
                    }

                    _context.Add(mec);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Greška pri dodavanju meča: " + ex.Message);
                    Console.WriteLine($"Exception: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                // Log validation errors
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        Console.WriteLine($"Validation error: {error.ErrorMessage}");
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.Sports = new SelectList(_context.Sportovi, "SportID", "Naziv", mec.SportID);
            return View(mec);
        }


        // POST: Mec/CreateFromModal
        // POST: Mec/CreateFromModal
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("CreateFromModal")]
        public async Task<IActionResult> CreateFromModal(int SportID, string Naslov, string Opis,
            string DatumVrijeme, string DatumVrijeme_Time, string Lokacija, int BrojIgraca)
        {
            // Spremamo informacije za dijagnostiku
            TempData["CreateAttempt"] = DateTime.Now.ToString();
            TempData["SportID"] = SportID.ToString();
            TempData["KorisnikID"] = HttpContext.Session.GetInt32("KorisnikID")?.ToString() ?? "null";
            TempData["IsAuthenticated"] = User.Identity.IsAuthenticated.ToString();

            try
            {
                // Parse date and time
                var datumString = DatumVrijeme;
                var vrijemeString = DatumVrijeme_Time;

                DateTime datum;
                if (!DateTime.TryParse(datumString, out datum))
                {
                    TempData["ErrorMessage"] = "Neispravan format datuma.";
                    return RedirectToAction("TestDatabase");
                }

                TimeSpan vrijeme;
                if (!TimeSpan.TryParse(vrijemeString, out vrijeme))
                {
                    TempData["ErrorMessage"] = "Neispravan format vremena.";
                    return RedirectToAction("TestDatabase");
                }

                // Combine date and time
                DateTime datumMeca = datum.Add(vrijeme);

                // Get current user ID
                var kreatorID = HttpContext.Session.GetInt32("KorisnikID");

                // If session is not set but user is authenticated, try to sync and set it
                if (kreatorID == null && User.Identity.IsAuthenticated)
                {
                    var email = User.Identity.Name;
                    TempData["SyncAttempt"] = "Da";
                    TempData["UserEmail"] = email;

                    // Sync the user
                    await _userSyncService.SyncIdentityUser(email);

                    // Try to get the korisnikID again from the database
                    var korisnik = await _context.Korisnici.FirstOrDefaultAsync(k => k.Email == email);
                    if (korisnik != null)
                    {
                        kreatorID = korisnik.KorisnikID;
                        HttpContext.Session.SetInt32("KorisnikID", kreatorID.Value);
                        HttpContext.Session.SetString("KorisnikIme", korisnik.Ime);
                        TempData["SyncResult"] = $"KorisnikID: {kreatorID}";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Korisnik nije pronađen nakon sinkronizacije.";
                        return RedirectToAction("TestDatabase");
                    }
                }
                else if (kreatorID == null)
                {
                    TempData["ErrorMessage"] = "Niste prijavljeni ili KorisnikID nije postavljen u sesiji.";
                    return RedirectToAction("TestDatabase");
                }

                // U CreateFromModal metodi
                var mec = new Mec
                {
                    Naslov = Naslov ?? $"Mec za sport ID {SportID}",
                    Opis = Opis ?? string.Empty,
                    KreatorID = kreatorID.Value,
                    DatumKreiranja = DateTime.Now,
                    DatumMeca = datumMeca,
                    Lokacija = Lokacija,
                    BrojIgraca = BrojIgraca,
                    SportID = SportID,
                    Status = StatusMeca.Objavljen,
                    Rezultat = "", // Postavite prazni string
                    KorisniciMeca = new List<MecKorisnik>
                    {
                        new MecKorisnik
                        {
                            KorisnikID = kreatorID.Value,
                            DatumMeca = DateTime.Now
                        }
                    },
                    Recenzije = new List<Recenzija>()
                };

                // Spremanje u bazu
                _context.Mecevi.Add(mec);
                var saveResult = await _context.SaveChangesAsync();
                TempData["SaveResult"] = saveResult.ToString();

                // Provjera je li meč stvarno spremljen
                var spremljeniMec = await _context.Mecevi.FindAsync(mec.MecID);
                if (spremljeniMec != null)
                {
                    TempData["SuccessMessage"] = $"Meč je uspješno kreiran! ID: {spremljeniMec.MecID}";
                    return RedirectToAction("MojiMecevi");
                }
                else
                {
                    TempData["ErrorMessage"] = "Meč nije pronađen nakon spremanja u bazu.";
                    return RedirectToAction("TestDatabase");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Greška prilikom kreiranja meča: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["InnerError"] = ex.InnerException.Message;
                }
                TempData["StackTrace"] = ex.StackTrace;
                return RedirectToAction("TestDatabase");
            }
        }


        // POST: Mec/Join/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int id)
        {
            // Get current user ID
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");

            // If session is not set but user is authenticated, try to sync and set it
            if (korisnikID == null && User.Identity.IsAuthenticated)
            {
                var email = User.Identity.Name;
                Console.WriteLine($"Join: User is authenticated with email {email}, but no KorisnikID in session");

                // Sync the user
                await _userSyncService.SyncIdentityUser(email);

                // Try to get the korisnikID again from the database
                var korisnik = await _context.Korisnici.FirstOrDefaultAsync(k => k.Email == email);
                if (korisnik != null)
                {
                    korisnikID = korisnik.KorisnikID;
                    HttpContext.Session.SetInt32("KorisnikID", korisnikID.Value);
                    HttpContext.Session.SetString("KorisnikIme", korisnik.Ime);
                    Console.WriteLine($"Join: Set KorisnikID in session to {korisnikID}");
                }
                else
                {
                    Console.WriteLine("Join: Failed to find or create Korisnik");
                    return Challenge();
                }
            }
            else if (korisnikID == null)
            {
                Console.WriteLine("Join: User is not authenticated and no KorisnikID in session");
                return Challenge();
            }

            // Check if the user has already sent a request for this match
            var postojeciZahtjev = await _context.MecRequests
                .AnyAsync(r => r.MecID == id && r.KorisnikID == korisnikID && r.Status == MecRequestStatus.Ceka);

            if (postojeciZahtjev)
            {
                // User has already sent a request, so don't allow direct join
                TempData["InfoMessage"] = "Vaš zahtjev za ovaj meč čeka odobrenje.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var mec = await _context.Mecevi
                .Include(m => m.KorisniciMeca)
                .FirstOrDefaultAsync(m => m.MecID == id);

            if (mec == null)
            {
                return NotFound();
            }

            // Check if user is already in the match
            if (mec.KorisniciMeca.Any(km => km.KorisnikID == korisnikID))
            {
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // Check if match is still open
            if (mec.Status != StatusMeca.Objavljen)
            {
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // Check if match is full
            if (mec.KorisniciMeca.Count >= mec.BrojIgraca)
            {
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // Add user to match
            mec.KorisniciMeca.Add(new MecKorisnik
            {
                KorisnikID = korisnikID.Value,
                DatumMeca = DateTime.Now
            });

            // If match is now full, update status
            if (mec.KorisniciMeca.Count >= mec.BrojIgraca)
            {
                mec.Status = StatusMeca.Dogovoren;
            }

            _context.Update(mec);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = id });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsCompleted(int id, string rezultat)
        {
            _logger.LogInformation("MarkAsCompleted započeo za meč ID={MecId}", id);

            // Get current user ID from Identity, NOT from session
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("MarkAsCompleted: Korisnik nije pronađen");
                return Challenge();
            }

            var email = user.Email;
            var korisnik = await _context.Korisnici.FirstOrDefaultAsync(k => k.Email == email);
            if (korisnik == null)
            {
                _logger.LogWarning("MarkAsCompleted: Korisnik s emailom {Email} nije pronađen u bazi", email);
                return Challenge();
            }

            var korisnikID = korisnik.KorisnikID;
            _logger.LogInformation("MarkAsCompleted: Dohvaćen korisnik ID={KorisnikId}", korisnikID);

            // First, load the match with all participants
            var mec = await _context.Mecevi
                .Include(m => m.KorisniciMeca)
                .FirstOrDefaultAsync(m => m.MecID == id);

            if (mec == null)
            {
                _logger.LogWarning("MarkAsCompleted: Meč ID={MecId} nije pronađen", id);
                return NotFound();
            }

            // Check if user is the creator
            if (korisnikID != mec.KreatorID)
            {
                _logger.LogWarning("MarkAsCompleted: Korisnik ID={KorisnikId} nije kreator meča ID={MecId}", korisnikID, id);
                return Challenge();
            }

            // Check if match can be completed
            if (mec.Status != StatusMeca.Dogovoren)
            {
                _logger.LogWarning("MarkAsCompleted: Meč ID={MecId} ima status {Status}, ne može se završiti", id, mec.Status);
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // Update match
            mec.Status = StatusMeca.Zavrsen;
            mec.Rezultat = rezultat;

            // Create MecConfirmation records for all participants
            var participants = mec.KorisniciMeca.ToList();
            foreach (var participant in participants)
            {
                var confirmation = new MecConfirmation
                {
                    MecID = id,
                    KorisnikID = participant.KorisnikID,
                    IsWinner = false,
                    ConfirmedAt = DateTime.Now
                };

                _context.MecConfirmations.Add(confirmation);
                _logger.LogInformation("MarkAsCompleted: Kreiran MecConfirmation za korisnika ID={KorisnikId}", participant.KorisnikID);
            }

            _context.Update(mec);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("MarkAsCompleted: Uspješno spremljene promjene za meč ID={MecId}", id);

                // Postavi vrijednost u TempData i u Cookie (kao rezervu)
                TempData["AktivniTab"] = "zavrseni";
                Response.Cookies.Append("AktivniTab", "zavrseni", new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddDays(1),
                    IsEssential = true
                });

                // Postavi vrijednost u sesiju, ali s dodatnim loggingom
                HttpContext.Session.SetString("AktivniTab", "zavrseni");
                _logger.LogInformation("MarkAsCompleted: AktivniTab postavljen u sesiju na 'zavrseni'");

                // Provjeri je li vrijednost stvarno postavljena
                var testValue = HttpContext.Session.GetString("AktivniTab");
                _logger.LogInformation("MarkAsCompleted: Provjera vrijednosti u sesiji - AktivniTab={TabValue}", testValue);

                TempData["SuccessMessage"] = "Meč je uspješno označen kao završen!";
                return RedirectToAction(nameof(MojiMecevi), new { tab = "zavrseni" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MarkAsCompleted: Greška pri završavanju meča ID={MecId}", id);
                TempData["ErrorMessage"] = "Greška pri završavanju meča. Molimo pokušajte ponovno.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
        }





        // POST: Mec/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MecID,Naslov,Opis,SportID,DatumMeca,Lokacija,BrojIgraca,Status")] Mec mec)
        {
            if (id != mec.MecID)
            {
                return NotFound();
            }

            // Remove validation errors for collections
            ModelState.Remove("KorisniciMeca");
            ModelState.Remove("Recenzije");
            ModelState.Remove("Sport");
            ModelState.Remove("Kreator");

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing match with collections
                    var postojeciMec = await _context.Mecevi
                        .Include(m => m.KorisniciMeca)
                        .Include(m => m.Recenzije)
                        .FirstOrDefaultAsync(m => m.MecID == id);

                    if (postojeciMec == null)
                    {
                        return NotFound();
                    }

                    // Check if user is the creator
                    var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
                    if (korisnikID == null || korisnikID != postojeciMec.KreatorID)
                    {
                        return RedirectToAction(nameof(Details), new { id = id });
                    }

                    // Update properties
                    postojeciMec.Naslov = mec.Naslov;
                    postojeciMec.Opis = mec.Opis;
                    postojeciMec.SportID = mec.SportID;
                    postojeciMec.DatumMeca = mec.DatumMeca;
                    postojeciMec.Lokacija = mec.Lokacija;
                    postojeciMec.BrojIgraca = mec.BrojIgraca;
                    postojeciMec.Status = mec.Status;

                    // Update in database
                    _context.Update(postojeciMec);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Details), new { id = id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MecExists(mec.MecID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Sports = new SelectList(_context.Sportovi, "SportID", "Naziv", mec.SportID);
            return View(mec);
        }

        // GET: Mec/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mec = await _context.Mecevi
                .Include(m => m.KorisniciMeca)
                .Include(m => m.Recenzije)
                .FirstOrDefaultAsync(m => m.MecID == id);

            if (mec == null)
            {
                return NotFound();
            }

            // Check if user is the creator
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null || korisnikID != mec.KreatorID)
            {
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // Prepare dropdown lists
            ViewBag.Sports = new SelectList(_context.Sportovi, "SportID", "Naziv", mec.SportID);
            ViewBag.StatusOptions = new SelectList(Enum.GetValues(typeof(StatusMeca))
                .Cast<StatusMeca>()
                .Select(s => new { Id = s, Name = s.ToString() }),
                "Id", "Name", mec.Status);

            return View(mec);
        }


        // GET: Mec/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mec = await _context.Mecevi
                .Include(m => m.Sport)
                .Include(m => m.Kreator)
                .FirstOrDefaultAsync(m => m.MecID == id);

            if (mec == null)
            {
                return NotFound();
            }

            // Check if user is the creator
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null || korisnikID != mec.KreatorID)
            {
                return RedirectToAction(nameof(Details), new { id = id });
            }

            return View(mec);
        }

        // POST: Mec/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mec = await _context.Mecevi.FindAsync(id);
            if (mec == null)
            {
                return NotFound();
            }

            // Check if user is the creator
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null || korisnikID != mec.KreatorID)
            {
                return RedirectToAction(nameof(Details), new { id = id });
            }

            _context.Mecevi.Remove(mec);
            await _context.SaveChangesAsync();

            // Change this line from:
            // return RedirectToAction(nameof(Index));
            // To:
            return RedirectToAction(nameof(MojiMecevi));
        }


        private bool MecExists(int id)
        {
            return _context.Mecevi.Any(e => e.MecID == id);
        }

        // GET: Mec/Feed
        [Authorize]
        public async Task<IActionResult> Feed()
        {
            // Get current user ID
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            // Get all public matches and private matches that include the current user
            // Exclude matches that are completed (Status == Zavrsen) or challenges (Status == CekaPrihvacanje)
            var sviMecevi = await _context.Mecevi
                .Include(m => m.Sport)
                .Include(m => m.Kreator)
                .Include(m => m.KorisniciMeca)
                .Where(m =>
                    m.Status == StatusMeca.Objavljen && // Only include published matches
                    (!m.JePrivatan || (m.JePrivatan && m.KorisniciMeca.Any(km => km.KorisnikID == korisnikID)))
                )
                .OrderByDescending(m => m.DatumKreiranja)
                .ToListAsync();

            // Dohvati sve mečeve za koje je korisnik već poslao zahtjev
            var postojeciZahtjevi = await _context.MecRequests
                .Where(r => r.KorisnikID == korisnikID && r.Status == MecRequestStatus.Ceka)
                .Select(r => r.MecID)
                .ToListAsync();

            ViewBag.CurrentUserId = korisnikID;
            ViewBag.SportOptions = await _context.Sportovi.ToListAsync();
            ViewBag.PoslanZahtjevZaMec = postojeciZahtjevi;

            return View(sviMecevi);
        }




        // GET: Mec/MojiMecevi
        [Authorize]
        public async Task<IActionResult> MojiMecevi(string tab = null)
        {
            _logger.LogInformation("MojiMecevi počinje izvršavanje, tab parametar={Tab}", tab);

            // 1. Prioritet: Eksplicitni URL parametar
            if (!string.IsNullOrEmpty(tab))
            {
                _logger.LogInformation("MojiMecevi: Koristi eksplicitni tab parametar '{Tab}'", tab);
                HttpContext.Session.SetString("AktivniTab", tab);
                Response.Cookies.Append("AktivniTab", tab, new CookieOptions { Expires = DateTimeOffset.Now.AddDays(1), IsEssential = true });
            }
            // 2. Prioritet: Vrijednost iz TempData
            else if (TempData["AktivniTab"] != null)
            {
                tab = TempData["AktivniTab"].ToString();
                _logger.LogInformation("MojiMecevi: Koristi tab iz TempData '{Tab}'", tab);
                HttpContext.Session.SetString("AktivniTab", tab);
                Response.Cookies.Append("AktivniTab", tab, new CookieOptions { Expires = DateTimeOffset.Now.AddDays(1), IsEssential = true });
            }
            // 3. Prioritet: Vrijednost iz sesije
            else if (HttpContext.Session.GetString("AktivniTab") != null)
            {
                tab = HttpContext.Session.GetString("AktivniTab");
                _logger.LogInformation("MojiMecevi: Koristi tab iz sesije '{Tab}'", tab);
            }
            // 4. Prioritet: Vrijednost iz kolačića
            else if (Request.Cookies.ContainsKey("AktivniTab"))
            {
                tab = Request.Cookies["AktivniTab"];
                _logger.LogInformation("MojiMecevi: Koristi tab iz kolačića '{Tab}'", tab);
                HttpContext.Session.SetString("AktivniTab", tab);
            }
            // Zadnji prioritet: Default vrijednost
            else
            {
                tab = "svi";
                _logger.LogInformation("MojiMecevi: Koristi default tab '{Tab}'", tab);
            }

            // Ostatak metode ostaje isti...

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("MojiMecevi: User nije pronađen");
                return Challenge();
            }

            var email = user.Email;
            var korisnik = await _context.Korisnici.FirstOrDefaultAsync(k => k.Email == email);
            if (korisnik == null)
            {
                Console.WriteLine("DEBUG: Korisnik nije pronađen u bazi");
                return Challenge();
            }

            var korisnikID = korisnik.KorisnikID;
            Console.WriteLine($"DEBUG: KorisnikID u korisniku: {korisnikID}");

            // Base query to get matches where the user is a participant
            var baseQuery = _context.Mecevi
                .Include(m => m.Sport)
                .Include(m => m.Kreator)
                .Include(m => m.KorisniciMeca)
                .Where(m =>
                    // User is the creator
                    m.KreatorID == korisnikID ||
                    // OR user is in the KorisniciMeca collection
                    m.KorisniciMeca.Any(km => km.KorisnikID == korisnikID)
                );

            // Create separate collections for each tab, with appropriate filtering
            List<Mec> allUserMatches;

            // Filter based on tab
            switch (tab)
            {
                case "zavrseni":
                    Console.WriteLine($"DEBUG ZAVRSENI: KorisnikID={korisnikID}, počinjem dohvat mečeva...");

                    // Get match IDs from MecConfirmation records for this user
                    var confirmationMatchIds = await _context.MecConfirmations
                        .Where(mc => mc.KorisnikID == korisnikID)
                        .Select(mc => mc.MecID)
                        .ToListAsync();

                    Console.WriteLine($"DEBUG ZAVRSENI: Pronađeno {confirmationMatchIds.Count} MecConfirmation zapisa");
                    foreach (var matchId in confirmationMatchIds)
                    {
                        Console.WriteLine($"DEBUG ZAVRSENI: MecConfirmation za MecID={matchId}");
                    }

                    // Get all matches that are either in the base query OR have a confirmation record
                    var completedMatches = await _context.Mecevi
                        .Include(m => m.Sport)
                        .Include(m => m.Kreator)
                        .Include(m => m.KorisniciMeca)
                        .Where(m =>
                            (m.Status == StatusMeca.Zavrsen &&
                            (m.KreatorID == korisnikID || m.KorisniciMeca.Any(km => km.KorisnikID == korisnikID))) ||
                            confirmationMatchIds.Contains(m.MecID))
                        .OrderByDescending(m => m.DatumKreiranja)
                        .ToListAsync();

                    Console.WriteLine($"DEBUG ZAVRSENI: Pronađeno ukupno {completedMatches.Count} završenih mečeva");
                    foreach (var match in completedMatches)
                    {
                        Console.WriteLine($"DEBUG ZAVRSENI: MecID={match.MecID}, Naslov='{match.Naslov}', " +
                                          $"Status={match.Status}, KreatorID={match.KreatorID}, " +
                                          $"BrojKorisnika={match.KorisniciMeca?.Count ?? 0}");

                        // Provjeri je li korisnik sudionik
                        bool jeKreator = match.KreatorID == korisnikID;
                        bool jeSudionik = match.KorisniciMeca?.Any(km => km.KorisnikID == korisnikID) ?? false;

                        Console.WriteLine($"DEBUG ZAVRSENI: MecID={match.MecID} - Korisnik je kreator: {jeKreator}, Korisnik je sudionik: {jeSudionik}");
                    }

                    allUserMatches = completedMatches;
                    break;

                case "kreirani":
                    // Only matches created by the user, excluding completed ones
                    allUserMatches = await baseQuery
                        .Where(m => m.KreatorID == korisnikID && m.Status != StatusMeca.Zavrsen)
                        .OrderByDescending(m => m.DatumKreiranja)
                        .ToListAsync();
                    break;

                case "dogovoreni":
                    // Only agreed matches
                    allUserMatches = await baseQuery
                        .Where(m => m.Status == StatusMeca.Dogovoren)
                        .OrderByDescending(m => m.DatumKreiranja)
                        .ToListAsync();
                    break;

                case "objavljeni":
                    // Only published matches
                    allUserMatches = await baseQuery
                        .Where(m => m.Status == StatusMeca.Objavljen)
                        .OrderByDescending(m => m.DatumKreiranja)
                        .ToListAsync();
                    break;

                case "svi":
                default:
                    // All matches except completed ones
                    allUserMatches = await baseQuery
                        .Where(m => m.Status != StatusMeca.Zavrsen)
                        .OrderByDescending(m => m.DatumKreiranja)
                        .ToListAsync();
                    break;
            }

            // Get pending requests separately
            var pendingRequestIds = await _context.MecRequests
                .Where(r => r.KorisnikID == korisnikID && r.Status == MecRequestStatus.Ceka)
                .Select(r => r.MecID)
                .ToListAsync();

            ViewBag.CurrentUserId = korisnikID;
            ViewBag.SportOptions = await _context.Sportovi.ToListAsync();
            ViewBag.CurrentTab = tab;
            ViewBag.PendingRequestIds = pendingRequestIds;

            return View(allUserMatches);
        }




        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult SetAktivniTab(string tab)
        {
            if (!string.IsNullOrEmpty(tab))
            {
                HttpContext.Session.SetString("AktivniTab", tab);

                // Također postavi kolačić kao rezervu
                Response.Cookies.Append("activeTab", tab, new CookieOptions
                {
                    Path = "/",
                    Expires = DateTimeOffset.Now.AddDays(1),
                    HttpOnly = false,
                    IsEssential = true
                });
            }
            return Json(new { success = true, tab = tab });
        }



        // GET: Mec/TestDatabase
        public async Task<IActionResult> TestDatabase()
        {
            var rezultati = new Dictionary<string, string>();

            try
            {
                // 1. Test baze podataka - dohvat sportova
                rezultati.Add("1. Pristup bazi podataka", "Pokušavam...");
                var sportovi = await _context.Sportovi.ToListAsync();
                rezultati["1. Pristup bazi podataka"] = $"Uspješno. Pronađeno {sportovi.Count} sportova.";

                // 2. Test sesije i korisnika
                rezultati.Add("2. Provjera sesije", "Pokušavam...");
                var korisnikId = HttpContext.Session.GetInt32("KorisnikID");
                var korisnikIme = HttpContext.Session.GetString("KorisnikIme");
                rezultati["2. Provjera sesije"] = $"KorisnikID: {korisnikId?.ToString() ?? "null"}, KorisnikIme: {korisnikIme ?? "null"}, Autentifikacija: {User.Identity.IsAuthenticated}";

                // 3. Provjera postojećih mečeva
                rezultati.Add("3. Provjera mečeva", "Pokušavam...");
                var brojMeceva = await _context.Mecevi.CountAsync();
                rezultati["3. Provjera mečeva"] = $"Broj mečeva u bazi: {brojMeceva}";

                // 4. Pokušaj kreiranje test meča
                if (korisnikId.HasValue)
                {
                    rezultati.Add("4. Kreiranje test meča", "Pokušavam...");

                    // Kreiraj test meč
                    var testMec = new Mec
                    {
                        Naslov = $"Test meč - {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}",
                        Opis = "Test meč kreiran direktno iz TestDatabase akcije",
                        DatumKreiranja = DateTime.Now,
                        DatumMeca = DateTime.Now.AddDays(1),
                        Lokacija = "Test lokacija",
                        BrojIgraca = 2,
                        SportID = 1, // Prvi sport
                        KreatorID = korisnikId.Value,
                        Status = StatusMeca.Objavljen,
                        KorisniciMeca = new List<MecKorisnik>
                {
                    new MecKorisnik
                    {
                        KorisnikID = korisnikId.Value,
                        DatumMeca = DateTime.Now
                    }
                }
                    };

                    _context.Mecevi.Add(testMec);
                    var rezultatSpremanja = await _context.SaveChangesAsync();

                    rezultati["4. Kreiranje test meča"] = $"Rezultat spremanja: {rezultatSpremanja} redaka. ID novog meča: {testMec.MecID}.";

                    // Provjeri je li meč stvarno spremljen
                    var spremljeniMec = await _context.Mecevi.FindAsync(testMec.MecID);
                    if (spremljeniMec != null)
                    {
                        rezultati.Add("5. Provjera kreiranog meča", $"Meč pronađen u bazi! ID: {spremljeniMec.MecID}, Naslov: {spremljeniMec.Naslov}");
                    }
                    else
                    {
                        rezultati.Add("5. Provjera kreiranog meča", "Meč NIJE pronađen u bazi nakon spremanja!");
                    }
                }
                else
                {
                    rezultati.Add("4. Kreiranje test meča", "Preskočeno - korisnik nije prijavljen ili KorisnikID nije postavljen.");
                }
            }
            catch (Exception ex)
            {
                rezultati.Add("GREŠKA", ex.Message);
                if (ex.InnerException != null)
                {
                    rezultati.Add("UNUTARNJA GREŠKA", ex.InnerException.Message);
                }
                rezultati.Add("STACK TRACE", ex.StackTrace);
            }

            // Dodaj u TestDatabase metodu za provjeru MecConfirmation zapisa
            rezultati.Add("6. Provjera MecConfirmation zapisa", "Pokušavam...");
            try
            {
                var korisnikId = HttpContext.Session.GetInt32("KorisnikID");
                if (korisnikId.HasValue)
                {
                    var mecConfirms = await _context.MecConfirmations
                        .Where(mc => mc.KorisnikID == korisnikId)
                        .ToListAsync();

                    rezultati["6. Provjera MecConfirmation zapisa"] =
                        $"Pronađeno {mecConfirms.Count} MecConfirmation zapisa za korisnika ID {korisnikId}";

                    if (mecConfirms.Any())
                    {
                        var mecIds = mecConfirms.Select(mc => mc.MecID).ToList();
                        var matchDetails = await _context.Mecevi
                            .Where(m => mecIds.Contains(m.MecID))
                            .Select(m => new { m.MecID, m.Naslov, m.Status })
                            .ToListAsync();

                        var matchInfo = string.Join(", ", matchDetails.Select(m =>
                            $"ID: {m.MecID}, Naslov: {m.Naslov}, Status: {m.Status}"));

                        rezultati.Add("7. Detalji potvrđenih mečeva", matchInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                rezultati.Add("6. Greška pri provjeri MecConfirmation", ex.Message);
            }


            return View(rezultati);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CreateChallenge(int izazvaniKorisnikID, string Naslov, string Opis,
    int SportID, string DatumVrijeme, string DatumVrijeme_Time, string Lokacija, bool JeIzazov)
        {
            // Dohvati trenutnog korisnika (izazivač)
            var kreatorID = HttpContext.Session.GetInt32("KorisnikID");
            Console.WriteLine($"DEBUG: CreateChallenge pokrenut - kreatorID={kreatorID}, izazvaniID={izazvaniKorisnikID}");

            if (kreatorID == null)
            {
                Console.WriteLine("DEBUG: Kreator nije pronađen u sesiji");
                return Challenge();
            }

            try
            {
                // Dodajte detaljno zapisivanje za sve parametre
                Console.WriteLine($"DEBUG: Parametri - Naslov={Naslov}, SportID={SportID}, Lokacija={Lokacija}");

                // Parse date and time
                var datumString = DatumVrijeme;
                var vrijemeString = DatumVrijeme_Time;

                DateTime datum;
                if (!DateTime.TryParse(datumString, out datum))
                {
                    Console.WriteLine($"DEBUG: Neispravan format datuma: {datumString}");
                    TempData["ErrorMessage"] = "Neispravan format datuma.";
                    return RedirectToAction("Index", "Korisnik");
                }

                TimeSpan vrijeme;
                if (!TimeSpan.TryParse(vrijemeString, out vrijeme))
                {
                    Console.WriteLine($"DEBUG: Neispravan format vremena: {vrijemeString}");
                    TempData["ErrorMessage"] = "Neispravan format vremena.";
                    return RedirectToAction("Index", "Korisnik");
                }

                // Combine date and time
                DateTime datumMeca = datum.Add(vrijeme);
                Console.WriteLine($"DEBUG: Datum meča: {datumMeca}");

                // Ostalo ostaje isto, nastavite pratiti svaki korak
                try
                {
                    // Kreiraj novi meč (tip izazov)
                    var mec = new Mec
                    {
                        Naslov = Naslov,
                        Opis = Opis ?? $"Izazov između korisnika",
                        KreatorID = kreatorID.Value,
                        DatumKreiranja = DateTime.Now,
                        DatumMeca = datumMeca,
                        Lokacija = Lokacija,
                        BrojIgraca = 2, // Fiksno na 2 za izazove
                        SportID = SportID,
                        Status = StatusMeca.CekaPrihvacanje, // Izazov čeka prihvaćanje
                        Rezultat = "",
                        JePrivatan = true, // Privatni meč
                        KorisniciMeca = new List<MecKorisnik>
                {
                    // Dodaj izazivača
                    new MecKorisnik
                    {
                        KorisnikID = kreatorID.Value,
                        DatumMeca = DateTime.Now,
                        JePrihvacen = true // Izazivač automatski prihvaća
                    }
                }
                    };

                    Console.WriteLine("DEBUG: Meč objekt kreiran, dodajem izazvanog korisnika");

                    // Dodaj izazvanog korisnika sa statusom neprihvaćeno
                    mec.KorisniciMeca.Add(new MecKorisnik
                    {
                        KorisnikID = izazvaniKorisnikID,
                        DatumMeca = DateTime.Now,
                        JePrihvacen = false // Izazvani korisnik još nije prihvatio
                    });

                    // Spremi u bazu
                    Console.WriteLine("DEBUG: Spremam meč u bazu");
                    _context.Mecevi.Add(mec);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"DEBUG: Meč uspješno spremljen, ID: {mec.MecID}");

                    Console.WriteLine("DEBUG: Kreiram notifikaciju izazova");
                    var result = await _notifikacijaService.KreirajNotifikacijuIzazovaAsync(
                        izazvaniKorisnikID,
                        mec.MecID,
                        kreatorID.Value);

                    Console.WriteLine($"DEBUG: Rezultat kreiranja notifikacije: {result}");

                    TempData["SuccessMessage"] = "Izazov je uspješno poslan! Čeka se prihvaćanje.";
                    return RedirectToAction("MojiMecevi", "Mec");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: Greška pri kreiranju meča: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"DEBUG: Inner exception: {ex.InnerException.Message}");
                    }
                    Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Vanjska greška u CreateChallenge: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"DEBUG: Inner exception: {ex.InnerException.Message}");
                }
                Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");

                TempData["ErrorMessage"] = $"Greška prilikom slanja izazova: {ex.Message}";
                return RedirectToAction("Index", "Korisnik");
            }
        }



        // POST: Mec/AcceptChallenge/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptChallenge(int id)
        {
            // Get current user ID
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            // Dohvati meč s korisnicima
            var mec = await _context.Mecevi
                .Include(m => m.KorisniciMeca)
                .FirstOrDefaultAsync(m => m.MecID == id && m.Status == StatusMeca.CekaPrihvacanje);

            if (mec == null)
            {
                return NotFound();
            }

            // Pronađi unos u MecKorisnik za trenutnog korisnika
            var mecKorisnik = mec.KorisniciMeca.FirstOrDefault(mk => mk.KorisnikID == korisnikID);

            if (mecKorisnik == null)
            {
                TempData["ErrorMessage"] = "Niste pozvani na ovaj izazov.";
                return RedirectToAction(nameof(MojiMecevi));
            }

            // Postavi status na prihvaćen
            mecKorisnik.JePrihvacen = true;
            mecKorisnik.DatumMeca = DateTime.Now; // Datum prihvaćanja

            // Provjeri jesu li svi igrači prihvatili
            if (mec.KorisniciMeca.All(mk => mk.JePrihvacen))
            {
                mec.Status = StatusMeca.Dogovoren;
            }

            _context.Update(mec);
            await _context.SaveChangesAsync();
            await _notifikacijaService.KreirajNotifikacijuPrihvacenogIzazovaAsync(
                mec.KreatorID,
                id,
                korisnikID.Value);

            TempData["SuccessMessage"] = "Izazov je uspješno prihvaćen!";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // POST: Mec/RejectChallenge/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectChallenge(int id)
        {
            // Get current user ID
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            // Dohvati meč s korisnicima
            var mec = await _context.Mecevi
                .Include(m => m.KorisniciMeca)
                .FirstOrDefaultAsync(m => m.MecID == id && m.Status == StatusMeca.CekaPrihvacanje);

            if (mec == null)
            {
                return NotFound();
            }

            // Provjeri je li korisnik izazvan
            var mecKorisnik = mec.KorisniciMeca.FirstOrDefault(mk => mk.KorisnikID == korisnikID);
            if (mecKorisnik == null)
            {
                TempData["ErrorMessage"] = "Niste pozvani na ovaj izazov.";
                return RedirectToAction(nameof(MojiMecevi));
            }

            // Prvo, pošalji notifikaciju kreatoru da je izazov odbijen prije brisanja
            await _notifikacijaService.KreirajNotifikacijuOdbijenogIzazovaAsync(
                mec.KreatorID,
                id,
                korisnikID.Value);

            // Umjesto da postavimo status na Zavrsen, potpuno brišemo meč iz baze
            // Prvo, ukloni povezane MecKorisnik zapise
            _context.MeceviKorisnici.RemoveRange(mec.KorisniciMeca);

            // Zatim ukloni sam meč
            _context.Mecevi.Remove(mec);

            // Spremi promjene u bazi
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Izazov je odbijen i uklonjen.";
            return RedirectToAction(nameof(MojiMecevi));
        }


        // POST: Mec/PosaljiZahtjev/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PosaljiZahtjev(int id)
        {
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            var rezultat = await _mecRequestService.PosaljiZahtjevAsync(korisnikID.Value, id);

            if (rezultat == null)
            {
                TempData["ErrorMessage"] = "Nije moguće poslati zahtjev za ovaj meč.";
            }
            else
            {
                TempData["SuccessMessage"] = "Zahtjev za sudjelovanje je uspješno poslan!";
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }

        // GET: Mec/ZahtjeviZaMec
        [Authorize]
        public async Task<IActionResult> ZahtjeviZaMec(int? mecId)
        {
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            // Dohvati sve primljene zahtjeve za mečeve koje je kreirao trenutni korisnik
            var zahtjevi = await _mecRequestService.DohvatiPrimljeneZahtjeveAsync(korisnikID.Value);

            // Ako je specificiran meč ID, filtriraj samo zahtjeve za taj meč
            if (mecId.HasValue)
            {
                zahtjevi = zahtjevi.Where(z => z.MecID == mecId.Value).ToList();

                // Provjeri je li korisnik kreator tog meča
                var mec = await _context.Mecevi.FindAsync(mecId.Value);
                if (mec == null || mec.KreatorID != korisnikID)
                {
                    return RedirectToAction(nameof(MojiMecevi));
                }

                ViewBag.MecId = mecId.Value;
                ViewBag.MecNaslov = mec.Naslov;
            }

            return View(zahtjevi);
        }

        // POST: Mec/PrihvatiZahtjev/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrihvatiZahtjev(int id)
        {
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            var rezultat = await _mecRequestService.AzurirajZahtjevAsync(id, MecRequestStatus.Prihvacen, korisnikID.Value);

            if (rezultat == null)
            {
                TempData["ErrorMessage"] = "Nije moguće prihvatiti ovaj zahtjev.";
            }
            else
            {
                TempData["SuccessMessage"] = "Zahtjev je prihvaćen!";
            }

            return RedirectToAction(nameof(ZahtjeviZaMec));
        }

        // POST: Mec/OdbijZahtjev/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OdbijZahtjev(int id)
        {
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            var rezultat = await _mecRequestService.AzurirajZahtjevAsync(id, MecRequestStatus.Odbijen, korisnikID.Value);

            if (rezultat == null)
            {
                TempData["ErrorMessage"] = "Nije moguće odbiti ovaj zahtjev.";
            }
            else
            {
                TempData["SuccessMessage"] = "Zahtjev je odbijen.";
            }

            return RedirectToAction(nameof(ZahtjeviZaMec));
        }

        // GET: Mec/MojiZahtjevi
        [Authorize]
        public async Task<IActionResult> MojiZahtjevi()
        {
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            var zahtjevi = await _mecRequestService.DohvatiPoslaneZahtjeveAsync(korisnikID.Value);
            return View(zahtjevi);
        }

        // GET: Mec/Admin
        public async Task<IActionResult> Admin()
        {
            // Prikaz svih mečeva sa povezanim podacima o kreatoru i sportu
            var mecevi = await _context.Mecevi
                .Include(m => m.Kreator)
                .Include(m => m.Sport)
                .ToListAsync();

            return View(mecevi);
        }

    }
}
