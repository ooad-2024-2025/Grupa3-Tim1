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
using QRCoder;
using System.IO;
using System.Drawing;
using System.Net.Http;

namespace Matchletic.Controllers
{
    public class MecController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserSyncService _userSyncService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly NotifikacijaService _notifikacijaService;
        private readonly MecRequestService _mecRequestService;

        public MecController(ApplicationDbContext context,
                             UserSyncService userSyncService,
                             UserManager<IdentityUser> userManager,
                             NotifikacijaService notifikacijaService,
                             MecRequestService mecRequestService)
        {
            _context = context;
            _userSyncService = userSyncService;
            _userManager = userManager;
            _notifikacijaService = notifikacijaService;
            _mecRequestService = mecRequestService;
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
                .Include(m => m.MecConfirmations)
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
                    Opis = Opis ?? $"Lokacija: {Lokacija}, Broj igrača: {BrojIgraca}",
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

        // POST: Mec/MarkAsCompleted/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsCompleted(int id, string rezultat, bool isWinner)
        {
            // Get current user ID
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");

            // If session is not set but user is authenticated, try to sync and set it
            if (korisnikID == null && User.Identity.IsAuthenticated)
            {
                var email = User.Identity.Name;
                Console.WriteLine($"MarkAsCompleted: User is authenticated with email {email}, but no KorisnikID in session");

                // Sync the user
                await _userSyncService.SyncIdentityUser(email);

                // Try to get the korisnikID again from the database
                var korisnik = await _context.Korisnici.FirstOrDefaultAsync(k => k.Email == email);
                if (korisnik != null)
                {
                    korisnikID = korisnik.KorisnikID;
                    HttpContext.Session.SetInt32("KorisnikID", korisnikID.Value);
                    HttpContext.Session.SetString("KorisnikIme", korisnik.Ime);
                    Console.WriteLine($"MarkAsCompleted: Set KorisnikID in session to {korisnikID}");
                }
                else
                {
                    Console.WriteLine("MarkAsCompleted: Failed to find or create Korisnik");
                    return Challenge();
                }
            }
            else if (korisnikID == null)
            {
                Console.WriteLine("MarkAsCompleted: User is not authenticated and no KorisnikID in session");
                return Challenge();
            }

            var mec = await _context.Mecevi.FindAsync(id);
            if (mec == null)
            {
                return NotFound();
            }

            // Check if user is the creator
            if (korisnikID != mec.KreatorID)
            {
                return Challenge();
            }

            // Check if match can be completed
            if (mec.Status != StatusMeca.Dogovoren)
            {
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // Update match
            mec.Status = StatusMeca.Zavrsen;
            mec.Rezultat = rezultat;

            Console.WriteLine($"MarkAsCompleted: Match ID {id} status set to {mec.Status}. Result: {rezultat}");

            // Add creator's confirmation
            var creatorConfirmation = new MecConfirmation
            {
                MecID = id,
                KorisnikID = korisnikID.Value,
                IsWinner = isWinner,
                ConfirmedAt = DateTime.Now
            };
            _context.MecConfirmations.Add(creatorConfirmation);

            _context.Update(mec);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = id });
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
            return RedirectToAction(nameof(Index));
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
            // Exclude matches that are completed (Status == Zavrsen)
            var sviMecevi = await _context.Mecevi
                .Include(m => m.Sport)
                .Include(m => m.Kreator)
                .Include(m => m.KorisniciMeca)
                .Where(m =>
                    m.Status != StatusMeca.Zavrsen && // Exclude completed matches
                    ((m.Status != StatusMeca.Dogovoren && !m.JePrivatan) ||
                    (m.JePrivatan && m.KorisniciMeca.Any(km => km.KorisnikID == korisnikID)))
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
        public async Task<IActionResult> MojiMecevi(string tab = "aktivni")
        {
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            // Get participating match IDs
            var participatingMatchIds = _context.MeceviKorisnici
                .Where(mk => mk.KorisnikID == korisnikID)
                .Select(mk => mk.MecID);

            // Base query
            var query = _context.Mecevi
                .Include(m => m.Sport)
                .Include(m => m.Kreator)
                .Include(m => m.KorisniciMeca)
                .Where(m => participatingMatchIds.Contains(m.MecID));

            // Filter based on tab
            if (tab == "zavrseni")
            {
                query = query.Where(m => m.Status == StatusMeca.Zavrsen);
            }
            else // "aktivni" tab or default
            {
                query = query.Where(m => m.Status != StatusMeca.Zavrsen);
            }

            var allUserMatches = await query
                .OrderByDescending(m => m.DatumKreiranja)
                .ToListAsync();

            ViewBag.CurrentUserId = korisnikID;
            ViewBag.SportOptions = await _context.Sportovi.ToListAsync();
            ViewBag.CurrentTab = tab;

            return View(allUserMatches);
        }

        // GET: Mec/UnesiRezultat/5
        //ovo
        [Authorize]
        public async Task<IActionResult> UnesiRezultat(int id)
        {
            var mec = await _context.Mecevi.FindAsync(id);
            if (mec == null) return NotFound();

            var userId = HttpContext.Session.GetInt32("KorisnikID");
            if (userId != mec.KreatorID) return Forbid();
            if (DateTime.Now < mec.DatumMeca) return BadRequest("Meč još nije završen.");
            if (mec.Status == StatusMeca.Zavrsen) return BadRequest("Rezultat već unesen.");

            return View(mec);
        }

        // POST: Mec/UnesiRezultat
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UnesiRezultat(int id, string rezultat)
        {
            var mec = await _context.Mecevi.FindAsync(id);
            var userId = HttpContext.Session.GetInt32("KorisnikID");
            if (mec == null || userId != mec.KreatorID) return Forbid();

            mec.Rezultat = rezultat;
            mec.Status = StatusMeca.Zavrsen;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Mec/GenerateQr/5
        [Authorize]
        [ResponseCache(Duration = 3600)] // Cache for 1 hour
        public async Task<IActionResult> GenerateQr(int id)
        {
            var mec = await _context.Mecevi.FindAsync(id);
            var userId = HttpContext.Session.GetInt32("KorisnikID");
            if (mec == null || userId != mec.KreatorID || mec.Status != StatusMeca.Zavrsen)
                return Forbid();

            // Get the current host and scheme
            var host = Request.Host.Value;
            var scheme = Request.IsHttps ? "https" : "http";
            var url = $"{scheme}://{host}{Url.Action("ConfirmResult", "Mec", new { id })}";
            Console.WriteLine($"GenerateQr: Generated URL: {url}");

            // Generate QR code locally using QRCoder
            using (var qrGenerator = new QRCoder.QRCodeGenerator())
            using (var qrCodeData = qrGenerator.CreateQrCode(url, QRCoder.QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCoder.PngByteQRCode(qrCodeData))
            {
                var qrCodeBytes = qrCode.GetGraphic(20);
                Console.WriteLine($"GenerateQr: QR Code Bytes Length: {qrCodeBytes.Length}");
                Response.Headers.Add("Cache-Control", "public, max-age=3600");
                return File(qrCodeBytes, "image/png");
            }
        }
        //ovoooo
        // GET: Mec/ConfirmResult/5
        [Authorize]
        public async Task<IActionResult> ConfirmResult(int id)
        {
            var mec = await _context.Mecevi
                .Include(m => m.KorisniciMeca)
                .ThenInclude(km => km.Korisnik)
                .Include(m => m.MecConfirmations)
                .FirstOrDefaultAsync(m => m.MecID == id);

            if (mec == null)
            {
                TempData["ErrorMessage"] = "Meč nije pronađen.";
                return RedirectToAction("Index", "Home");
            }

            if (mec.Status != StatusMeca.Zavrsen)
            {
                TempData["ErrorMessage"] = "Meč još nije završen.";
                return RedirectToAction("Details", new { id });
            }

            var userId = HttpContext.Session.GetInt32("KorisnikID");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Morate biti prijavljeni da biste potvrdili rezultat.";
                return RedirectToAction("Login", "Account");
            }

            if (!mec.KorisniciMeca.Any(k => k.KorisnikID == userId))
            {
                TempData["ErrorMessage"] = "Samo sudionici meča mogu potvrditi rezultat.";
                return RedirectToAction("Details", new { id });
            }

            // Provjeri postoje li već unosi za tog korisnika
            var existing = mec.MecConfirmations
                              .FirstOrDefault(c => c.KorisnikID == userId);
            if (existing != null)
            {
                // vraćamo već spremljeni entitet
                return View(existing);
            }

            // pripremi novi objekt s već postavljenim MecID i KorisnikID
            var confirmation = new MecConfirmation
            {
                MecID = id,
                KorisnikID = userId.Value
            };
            return View(confirmation);
        }
        // POST: Mec/ConfirmResult
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ConfirmResult(MecConfirmation model)
        {
            // Provjeri da korisnik ne laže u hidden poljima
            var userId = HttpContext.Session.GetInt32("KorisnikID");
            if (userId == null || model.KorisnikID != userId.Value)
                return Forbid();

            // Validacija atributima na MecConfirmation (npr. [Required])
            if (!ModelState.IsValid)
                return View(model);

            // Provjeri postoji li već zapis za tog korisnika
            var existing = await _context.MecConfirmations
                .FirstOrDefaultAsync(c => c.MecID == model.MecID
                                       && c.KorisnikID == userId);
            if (existing == null)
            {
                model.ConfirmedAt = DateTime.Now;
                _context.MecConfirmations.Add(model);
            }
            else
            {
                existing.IsWinner = model.IsWinner;
                existing.ConfirmedAt = DateTime.Now;
                _context.MecConfirmations.Update(existing);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = model.MecID });
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

            return View(rezultati);
        }

        // POST: Mec/CreateChallenge
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CreateChallenge(int izazvaniKorisnikID, string Naslov, string Opis,
            int SportID, string DatumVrijeme, string DatumVrijeme_Time, string Lokacija, bool JeIzazov)
        {
            // Dohvati trenutnog korisnika (izazivač)
            var kreatorID = HttpContext.Session.GetInt32("KorisnikID");
            if (kreatorID == null)
            {
                return Challenge();
            }

            try
            {
                // Parse date and time
                var datumString = DatumVrijeme;
                var vrijemeString = DatumVrijeme_Time;

                DateTime datum;
                if (!DateTime.TryParse(datumString, out datum))
                {
                    TempData["ErrorMessage"] = "Neispravan format datuma.";
                    return RedirectToAction("Index", "Korisnik");
                }

                TimeSpan vrijeme;
                if (!TimeSpan.TryParse(vrijemeString, out vrijeme))
                {
                    TempData["ErrorMessage"] = "Neispravan format vremena.";
                    return RedirectToAction("Index", "Korisnik");
                }

                // Combine date and time
                DateTime datumMeca = datum.Add(vrijeme);

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

                // Dodaj izazvanog korisnika sa statusom neprihvaćeno
                mec.KorisniciMeca.Add(new MecKorisnik
                {
                    KorisnikID = izazvaniKorisnikID,
                    DatumMeca = DateTime.Now,
                    JePrihvacen = false // Izazvani korisnik još nije prihvatio
                });

                // Spremi u bazu
                _context.Mecevi.Add(mec);
                await _context.SaveChangesAsync();
                await _notifikacijaService.KreirajNotifikacijuIzazovaAsync(
                    izazvaniKorisnikID,
                    mec.MecID,
                    kreatorID.Value);

                TempData["SuccessMessage"] = "Izazov je uspješno poslan! Čeka se prihvaćanje.";
                return RedirectToAction("MojiMecevi", "Mec");
            }
            catch (Exception ex)
            {
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

            // Dohvati meč
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

            // Ukloni korisnika iz meča
            mec.KorisniciMeca.Remove(mecKorisnik);

            // Ako je izazvani odbio, otkaži cijeli meč
            if (mec.Status == StatusMeca.CekaPrihvacanje)
            {
                // Otkaži meč - samo izmijeni status ako želiš zadržati povijesne podatke
                mec.Status = StatusMeca.Zavrsen;
                mec.Rezultat = "Izazov odbijen";
                _context.Update(mec);
            }
            else
            {
                // U ostalim slučajevima samo ukloni korisnika
                _context.MeceviKorisnici.Remove(mecKorisnik);
            }

            await _context.SaveChangesAsync();
            await _notifikacijaService.KreirajNotifikacijuOdbijenogIzazovaAsync(
                mec.KreatorID,
                id,
                korisnikID.Value);

            TempData["SuccessMessage"] = "Izazov je odbijen.";
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
    }
}
