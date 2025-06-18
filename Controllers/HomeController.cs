using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Matchletic.Models;
using Matchletic.Data;

namespace Matchletic.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");

            // Get all published matches for the feed
            var objavljeniMecevi = await _context.Mecevi
                .Include(m => m.Sport)
                .Include(m => m.Kreator)
                .Include(m => m.KorisniciMeca)
                .Where(m => m.Status == StatusMeca.Objavljen)
                .OrderByDescending(m => m.DatumKreiranja)
                .Take(6) // Limit to 6 most recent
                .ToListAsync();

            // Ukupan broj svih objavljenih mečeva
            ViewBag.Objavljeni = await _context.Mecevi.CountAsync(m => m.Status == StatusMeca.Objavljen);

            // If user is authenticated, get their matches counts
            if (korisnikID.HasValue)
            {
                // Get user's matches
                var userMecevi = await _context.Mecevi
                    .Where(m => m.KorisniciMeca.Any(km => km.KorisnikID == korisnikID))
                    .ToListAsync();

                // Broj objavljenih mečeva koje je korisnik kreirao
                ViewBag.Objavljeni = userMecevi.Count(m => m.Status == StatusMeca.Objavljen && m.KreatorID == korisnikID);
                ViewBag.Dogovoreni = userMecevi.Count(m => m.Status == StatusMeca.Dogovoren);
                ViewBag.Zavrseni = userMecevi.Count(m => m.Status == StatusMeca.Zavrsen);

                // Add statistics data
                ViewBag.MatchesPlayed = userMecevi.Count(m => m.Status == StatusMeca.Zavrsen);

                // Calculate win rate (you'd need to implement logic to determine wins)
                var totalFinishedMatches = userMecevi.Count(m => m.Status == StatusMeca.Zavrsen);
                var winRate = totalFinishedMatches > 0 ? 68 : 0; // Example: replace with actual calculation
                ViewBag.WinRate = winRate;

                // Rating would come from somewhere like user reviews
                // For now, using placeholder value
                ViewBag.Rating = 4.5;

                // User rank would typically come from a leaderboard calculation
                ViewBag.Rank = "1,234";
            }
            else
            {
                // Set default values for non-authenticated users
                ViewBag.MatchesPlayed = 0;
                ViewBag.WinRate = 0;
                ViewBag.Rating = 0;
                ViewBag.Rank = "-";
            }

            ViewBag.CurrentUserId = korisnikID;
            ViewBag.SportOptions = await _context.Sportovi.ToListAsync();
            ViewBag.ActiveTab = "feed"; // Default active tab

            return View(objavljeniMecevi);
        }
    }
}
