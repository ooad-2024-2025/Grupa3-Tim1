using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Matchletic.Data;
using Matchletic.Models;

namespace Matchletic.Controllers
{
    public class AccountsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Accounts/Login
        public IActionResult Login(string returnUrl = null)
        {
            // Redirect to the Identity login page
            returnUrl = returnUrl ?? Url.Content("~/");
            return LocalRedirect($"/Identity/Account/Login?ReturnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        // GET: Accounts/Register
        public IActionResult Register()
        {
            // Redirect to the Identity register page
            return LocalRedirect("/Identity/Account/Register");
        }

        // POST: Accounts/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Redirect to the Identity logout page
            return LocalRedirect("/Identity/Account/Logout");
        }

        // GET: Accounts/AccessDenied
        public IActionResult AccessDenied()
        {
            // Redirect to the Identity access denied page
            return LocalRedirect("/Identity/Account/AccessDenied");
        }

        // GET: Accounts/Profile
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("KorisnikID");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var korisnik = await _context.Korisnici
                .Include(k => k.KorisnickiSportovi)
                .ThenInclude(ks => ks.Sport)
                .FirstOrDefaultAsync(k => k.KorisnikID == userId);

            if (korisnik == null)
            {
                return NotFound();
            }

            return View(korisnik);
        }

        // GET: Accounts/Edit
        public async Task<IActionResult> Edit()
        {
            var userId = HttpContext.Session.GetInt32("KorisnikID");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var korisnik = await _context.Korisnici.FindAsync(userId);
            if (korisnik == null)
            {
                return NotFound();
            }

            return View(korisnik);
        }

        // POST: Accounts/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Korisnik model)
        {
            var userId = HttpContext.Session.GetInt32("KorisnikID");
            if (userId == null || userId != model.KorisnikID)
            {
                return RedirectToAction("Login");
            }

            // Remove validation errors for collections
            ModelState.Remove("MeceviKorisnika");
            ModelState.Remove("NapisaneRecenzije");
            ModelState.Remove("KorisnickiSportovi");

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing user with collections
                    var postojeciKorisnik = await _context.Korisnici
                        .Include(k => k.MeceviKorisnika)
                        .Include(k => k.NapisaneRecenzije)
                        .Include(k => k.KorisnickiSportovi)
                        .FirstOrDefaultAsync(k => k.KorisnikID == userId);

                    if (postojeciKorisnik == null)
                    {
                        return NotFound();
                    }

                    // Update properties
                    postojeciKorisnik.Ime = model.Ime;
                    postojeciKorisnik.Prezime = model.Prezime;
                    postojeciKorisnik.Email = model.Email;
                    postojeciKorisnik.ProfilnaSlika = model.ProfilnaSlika;

                    // Only update password if provided
                    if (!string.IsNullOrEmpty(model.Lozinka))
                    {
                        postojeciKorisnik.Lozinka = model.Lozinka;
                    }

                    _context.Update(postojeciKorisnik);
                    await _context.SaveChangesAsync();

                    // Update session
                    HttpContext.Session.SetString("KorisnikIme", postojeciKorisnik.Ime);

                    return RedirectToAction(nameof(Profile));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Korisnici.AnyAsync(k => k.KorisnikID == model.KorisnikID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(model);
        }
    }
}
