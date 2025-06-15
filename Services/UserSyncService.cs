// Services/UserSyncService.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Matchletic.Data;
using Matchletic.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Matchletic.Services
{
    public class UserSyncService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UserSyncService(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Korisnik> SyncIdentityUser(string email, string ime = null, string prezime = null, string lokacija = null)
        {
            if (string.IsNullOrEmpty(email))
            {
                Console.WriteLine("SyncIdentityUser: Email is null or empty");
                return null;
            }

            try
            {
                // Provjeri postoji li već korisnik u Korisnici tabeli
                var korisnik = await _context.Korisnici.FirstOrDefaultAsync(k => k.Email == email);

                if (korisnik == null)
                {
                    // Ako ne postoji, kreiraj novi zapis
                    var noviKorisnik = new Korisnik
                    {
                        Email = email,
                        Ime = ime ?? "Korisnik", // Koristi ime ako je dostupno
                        Prezime = prezime ?? "", // Koristi prezime ako je dostupno
                        Lokacija = lokacija ?? "", // Koristi lokaciju ako je dostupna
                        Lozinka = "IdentityAuth", // Napomena: autentifikacija ide kroz Identity
                        Aktivan = true,
                        JeAdmin = false,
                        ProfilnaSlika = "",
                        Ocjena = 0.0,
                        KorisnickiSportovi = new List<KorisnikSport>(),
                        MeceviKorisnika = new List<MecKorisnik>(),
                        NapisaneRecenzije = new List<Recenzija>()
                    };

                    _context.Korisnici.Add(noviKorisnik);
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"Created new Korisnik with ID: {noviKorisnik.KorisnikID} for email: {email}");
                    return noviKorisnik;
                }
                else
                {
                    Console.WriteLine($"Korisnik already exists with ID: {korisnik.KorisnikID} for email: {email}");

                    // Ažuriraj podatke ako su dostupni a nisu postavljeni
                    bool promjene = false;

                    if (!string.IsNullOrEmpty(ime) && korisnik.Ime == "Korisnik")
                    {
                        korisnik.Ime = ime;
                        promjene = true;
                    }

                    if (!string.IsNullOrEmpty(prezime) && string.IsNullOrEmpty(korisnik.Prezime))
                    {
                        korisnik.Prezime = prezime;
                        promjene = true;
                    }

                    if (!string.IsNullOrEmpty(lokacija) && string.IsNullOrEmpty(korisnik.Lokacija))
                    {
                        korisnik.Lokacija = lokacija;
                        promjene = true;
                    }

                    if (promjene)
                    {
                        _context.Update(korisnik);
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"Updated existing Korisnik with ID: {korisnik.KorisnikID}");
                    }

                    return korisnik;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SyncIdentityUser: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }


    }
}
