// Middlewares/SessionSyncMiddleware.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Matchletic.Data;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Matchletic.Services;
using System;

namespace Matchletic.Middlewares
{
    public class SessionSyncMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionSyncMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<IdentityUser> userManager,
            ApplicationDbContext dbContext, UserSyncService userSyncService)
        {
            try
            {
                // Ako je korisnik prijavljen kroz Identity
                if (context.User.Identity.IsAuthenticated)
                {
                    // Provjeri da li je KorisnikID već postavljen u sesiji
                    if (!context.Session.Keys.Contains("KorisnikID"))
                    {
                        // Dobavi email trenutnog korisnika
                        var email = context.User.Identity.Name;
                        Console.WriteLine($"SessionSyncMiddleware: Authenticated user email: {email}");

                        // Sync user kroz servis
                        await userSyncService.SyncIdentityUser(email);

                        // Pronađi Korisnik objekt koji odgovara tom emailu
                        var korisnik = await dbContext.Korisnici.FirstOrDefaultAsync(k => k.Email == email);

                        if (korisnik != null)
                        {
                            Console.WriteLine($"SessionSyncMiddleware: Found Korisnik with ID: {korisnik.KorisnikID}, Name: {korisnik.Ime}");
                            // Postavi KorisnikID i ostale podatke u sesiju
                            context.Session.SetInt32("KorisnikID", korisnik.KorisnikID);
                            context.Session.SetString("KorisnikIme", korisnik.Ime);
                            // Dodajte druge sesijske podatke ako su potrebni
                        }
                        else
                        {
                            Console.WriteLine($"SessionSyncMiddleware: No Korisnik found for email: {email}");

                            // Dodatna provjera korisnika u bazi
                            try
                            {
                                var korisnici = await dbContext.Korisnici.ToListAsync();
                                Console.WriteLine($"SessionSyncMiddleware: Total users in database: {korisnici.Count}");
                                foreach (var k in korisnici.Take(5)) // Prikaži samo prvih 5 korisnika
                                {
                                    Console.WriteLine($"SessionSyncMiddleware: Korisnik in DB - ID: {k.KorisnikID}, Email: {k.Email}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error during diagnostic logging: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        var userId = context.Session.GetInt32("KorisnikID");
                        Console.WriteLine($"SessionSyncMiddleware: KorisnikID already in session: {userId}");
                    }
                }
                else
                {
                    Console.WriteLine("SessionSyncMiddleware: User is not authenticated");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SessionSyncMiddleware: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            // Nastavi s izvršavanjem pipeline-a
            await _next(context);
        }
    }

    // Extension metoda za lakše dodavanje middleware-a
    public static class SessionSyncMiddlewareExtensions
    {
        public static IApplicationBuilder UseSessionSync(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SessionSyncMiddleware>();
        }
    }
}
