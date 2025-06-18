// Services/NotifikacijaService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Matchletic.Data;
using Matchletic.Models;
using Microsoft.EntityFrameworkCore;

namespace Matchletic.Services
{
    public class NotifikacijaService
    {
        private readonly ApplicationDbContext _context;

        public NotifikacijaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> KreirajNotifikacijuIzazovaAsync(int korisnikID, int mecID, int kreatorID)
        {
            try
            {
                // Dodajte detaljno zapisivanje
                Console.WriteLine($"DEBUG: Kreiranje notifikacije izazova - korisnikID={korisnikID}, mecID={mecID}, kreatorID={kreatorID}");

                var kreator = await _context.Korisnici.FindAsync(kreatorID);
                var mec = await _context.Mecevi.FindAsync(mecID);

                Console.WriteLine($"DEBUG: Kreator pronađen: {kreator != null}, Meč pronađen: {mec != null}");

                if (kreator != null && mec != null)
                {
                    var notifikacija = new Notifikacija
                    {
                        KorisnikID = korisnikID,
                        Naslov = "Novi izazov",
                        Sadrzaj = $"{kreator.Ime} {kreator.Prezime} te izazvao na meč: {mec.Naslov}",
                        Url = $"/Mec/Details/{mecID}",
                        Tip = NotifikacijaTip.Izazov,
                        DatumKreiranja = DateTime.Now
                    };

                    try
                    {
                        _context.Notifikacije.Add(notifikacija);
                        await _context.SaveChangesAsync();
                        Console.WriteLine("DEBUG: Notifikacija uspješno spremljena u bazu");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"DEBUG: Greška pri spremanju notifikacije: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"DEBUG: Inner exception: {ex.InnerException.Message}");
                        }
                        throw;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Greška pri kreiranju notifikacije: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"DEBUG: Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }



        public async Task KreirajNotifikacijuPrihvacenogIzazovaAsync(int kreatorID, int mecID, int prihvatioID)
        {
            var prihvatio = await _context.Korisnici.FindAsync(prihvatioID);
            var mec = await _context.Mecevi.FindAsync(mecID);

            if (prihvatio != null && mec != null)
            {
                var notifikacija = new Notifikacija
                {
                    KorisnikID = kreatorID,
                    Naslov = "Izazov prihvaćen",
                    Sadrzaj = $"{prihvatio.Ime} {prihvatio.Prezime} je prihvatio tvoj izazov za meč: {mec.Naslov}",
                    Url = $"/Mec/Details/{mecID}",
                    Tip = NotifikacijaTip.PrihvacenIzazov,
                    DatumKreiranja = DateTime.Now
                };

                _context.Notifikacije.Add(notifikacija);
                await _context.SaveChangesAsync();
            }
        }

        public async Task KreirajNotifikacijuOdbijenogIzazovaAsync(int kreatorID, int mecID, int odbioID)
        {
            var odbio = await _context.Korisnici.FindAsync(odbioID);
            var mec = await _context.Mecevi.FindAsync(mecID);

            if (odbio != null && mec != null)
            {
                var notifikacija = new Notifikacija
                {
                    KorisnikID = kreatorID,
                    Naslov = "Izazov odbijen",
                    Sadrzaj = $"{odbio.Ime} {odbio.Prezime} je odbio tvoj izazov za meč: {mec.Naslov}",
                    Url = $"/Mec/Details/{mecID}",
                    Tip = NotifikacijaTip.OdbijenIzazov,
                    DatumKreiranja = DateTime.Now
                };

                _context.Notifikacije.Add(notifikacija);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetBrojNeprocitanihNotifikacijaAsync(int korisnikID)
        {
            return await _context.Notifikacije
                .Where(n => n.KorisnikID == korisnikID && !n.Procitano)
                .CountAsync();
        }

        public async Task<List<Notifikacija>> GetNotifikacijeZaKorisnikaAsync(int korisnikID, int count = 10)
        {
            return await _context.Notifikacije
                .Where(n => n.KorisnikID == korisnikID)
                .OrderByDescending(n => n.DatumKreiranja)
                .Take(count)
                .ToListAsync();
        }

        public async Task OznaciKaoProcitanoAsync(int notifikacijaID)
        {
            var notifikacija = await _context.Notifikacije.FindAsync(notifikacijaID);
            if (notifikacija != null)
            {
                notifikacija.Procitano = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task OznaciSveKaoProcitanoAsync(int korisnikID)
        {
            var notifikacije = await _context.Notifikacije
                .Where(n => n.KorisnikID == korisnikID && !n.Procitano)
                .ToListAsync();

            foreach (var notifikacija in notifikacije)
            {
                notifikacija.Procitano = true;
            }

            await _context.SaveChangesAsync();
        }

        // Services/NotifikacijaService.cs - dodajte ove metode na kraj postojeće klase

        public async Task KreirajNotifikacijuZahtjevaZaMecAsync(int korisnikID, int posiljateljID, int mecID, int zahtjevID)
        {
            var posiljatelj = await _context.Korisnici.FindAsync(posiljateljID);
            var mec = await _context.Mecevi.FindAsync(mecID);

            if (posiljatelj != null && mec != null)
            {
                var notifikacija = new Notifikacija
                {
                    KorisnikID = korisnikID,
                    Naslov = "Novi zahtjev za meč",
                    Sadrzaj = $"{posiljatelj.Ime} {posiljatelj.Prezime} želi sudjelovati u vašem meču: {mec.Naslov}",
                    Url = $"/Mec/ZahtjeviZaMec?mecId={mecID}",
                    Tip = NotifikacijaTip.ZahtjevZaMec,
                    DatumKreiranja = DateTime.Now,
                    PodaciZahtjeva = zahtjevID.ToString() // Spremi ID zahtjeva u podatke notifikacije
                };

                _context.Notifikacije.Add(notifikacija);
                await _context.SaveChangesAsync();
            }
        }

        public async Task KreirajNotifikacijuOdgovoraNaZahtjevAsync(int korisnikID, int mecID, int zahtjevID, bool prihvacen)
        {
            var mec = await _context.Mecevi.FindAsync(mecID);

            if (mec != null)
            {
                var notifikacija = new Notifikacija
                {
                    KorisnikID = korisnikID,
                    Naslov = prihvacen ? "Zahtjev prihvaćen" : "Zahtjev odbijen",
                    Sadrzaj = prihvacen
                        ? $"Vaš zahtjev za sudjelovanje u meču '{mec.Naslov}' je prihvaćen!"
                        : $"Vaš zahtjev za sudjelovanje u meču '{mec.Naslov}' je odbijen.",
                    Url = prihvacen ? $"/Mec/Details/{mecID}" : "/Mec/Feed",
                    Tip = prihvacen ? NotifikacijaTip.ZahtjevPrihvacen : NotifikacijaTip.ZahtjevOdbijen,
                    DatumKreiranja = DateTime.Now,
                    PodaciZahtjeva = zahtjevID.ToString() // Spremi ID zahtjeva u podatke notifikacije
                };

                _context.Notifikacije.Add(notifikacija);
                await _context.SaveChangesAsync();
            }
        }
    }
}
