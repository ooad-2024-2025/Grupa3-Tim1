// Services/MecRequestService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Matchletic.Data;
using Matchletic.Models;
using Microsoft.EntityFrameworkCore;

namespace Matchletic.Services
{
    public class MecRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly NotifikacijaService _notifikacijaService;

        public MecRequestService(ApplicationDbContext context, NotifikacijaService notifikacijaService)
        {
            _context = context;
            _notifikacijaService = notifikacijaService;
        }

        // Pošalji zahtjev za sudjelovanje u meču
        public async Task<MecRequest> PosaljiZahtjevAsync(int korisnikID, int mecID)
        {
            // Provjeri postoji li već zahtjev
            var postojeciZahtjev = await _context.MecRequests
                .FirstOrDefaultAsync(r => r.KorisnikID == korisnikID && r.MecID == mecID);

            if (postojeciZahtjev != null)
            {
                return postojeciZahtjev; // Već postoji zahtjev
            }

            // Provjeri je li meč još uvijek otvoren za prijave
            var mec = await _context.Mecevi
                .Include(m => m.KorisniciMeca)
                .FirstOrDefaultAsync(m => m.MecID == mecID);

            if (mec == null || mec.Status != StatusMeca.Objavljen)
            {
                return null; // Meč nije dostupan za prijave
            }

            // Provjeri je li korisnik već dio meča
            if (mec.KorisniciMeca.Any(km => km.KorisnikID == korisnikID))
            {
                return null; // Korisnik već sudjeluje u meču
            }

            // Provjeri ima li još mjesta
            if (mec.KorisniciMeca.Count >= mec.BrojIgraca)
            {
                return null; // Meč je popunjen
            }

            // Kreiraj novi zahtjev
            var zahtjev = new MecRequest
            {
                KorisnikID = korisnikID,
                MecID = mecID,
                Status = MecRequestStatus.Ceka,
                DatumKreiranja = DateTime.Now
            };

            _context.MecRequests.Add(zahtjev);
            await _context.SaveChangesAsync();

            // Pošalji notifikaciju kreatoru meča
            await _notifikacijaService.KreirajNotifikacijuZahtjevaZaMecAsync(
                mec.KreatorID,  // primatelj notifikacije (kreator meča)
                korisnikID,     // pošiljatelj zahtjeva
                mecID,          // ID meča
                zahtjev.MecRequestID // ID zahtjeva
            );

            return zahtjev;
        }

        // Ažuriraj status zahtjeva (prihvati/odbij)
        public async Task<MecRequest> AzurirajZahtjevAsync(int zahtjevID, string noviStatus, int trenutniKorisnikID)
        {
            var zahtjev = await _context.MecRequests
                .Include(r => r.Mec)
                .FirstOrDefaultAsync(r => r.MecRequestID == zahtjevID);

            if (zahtjev == null)
            {
                return null;
            }

            // Provjeri je li trenutni korisnik kreator meča
            if (zahtjev.Mec.KreatorID != trenutniKorisnikID)
            {
                return null; // Samo kreator meča može ažurirati zahtjeve
            }

            // Ažuriraj zahtjev
            zahtjev.Status = noviStatus;
            zahtjev.DatumAzuriranja = DateTime.Now;

            // Ako je zahtjev prihvaćen, dodaj korisnika u meč
            if (noviStatus == MecRequestStatus.Prihvacen)
            {
                var mec = await _context.Mecevi
                    .Include(m => m.KorisniciMeca)
                    .FirstOrDefaultAsync(m => m.MecID == zahtjev.MecID);

                if (mec != null)
                {
                    // Dodaj korisnika u meč
                    mec.KorisniciMeca.Add(new MecKorisnik
                    {
                        KorisnikID = zahtjev.KorisnikID,
                        MecID = mec.MecID,
                        DatumMeca = DateTime.Now,
                        JePrihvacen = true
                    });

                    // Ako je meč sada popunjen, ažuriraj status
                    if (mec.KorisniciMeca.Count >= mec.BrojIgraca)
                    {
                        mec.Status = StatusMeca.Dogovoren;
                    }
                }

                // Pošalji notifikaciju korisniku da je njegov zahtjev prihvaćen
                await _notifikacijaService.KreirajNotifikacijuOdgovoraNaZahtjevAsync(
                    zahtjev.KorisnikID,
                    zahtjev.MecID,
                    zahtjev.MecRequestID,
                    true // prihvaćen
                );
            }
            else if (noviStatus == MecRequestStatus.Odbijen)
            {
                // Pošalji notifikaciju korisniku da je njegov zahtjev odbijen
                await _notifikacijaService.KreirajNotifikacijuOdgovoraNaZahtjevAsync(
                    zahtjev.KorisnikID,
                    zahtjev.MecID,
                    zahtjev.MecRequestID,
                    false // odbijen
                );
            }

            await _context.SaveChangesAsync();
            return zahtjev;
        }

        // Dohvati zahtjeve koje je korisnik poslao
        public async Task<List<MecRequest>> DohvatiPoslaneZahtjeveAsync(int korisnikID)
        {
            return await _context.MecRequests
                .Include(r => r.Mec)
                    .ThenInclude(m => m.Sport)
                .Include(r => r.Mec)
                    .ThenInclude(m => m.Kreator)
                .Where(r => r.KorisnikID == korisnikID && r.Status == MecRequestStatus.Ceka)
                .OrderByDescending(r => r.DatumKreiranja)
                .ToListAsync();
        }

        // Dohvati zahtjeve koje je korisnik primio (kao kreator meča)
        public async Task<List<MecRequest>> DohvatiPrimljeneZahtjeveAsync(int korisnikID)
        {
            return await _context.MecRequests
                .Include(r => r.Mec)
                    .ThenInclude(m => m.Sport)
                .Include(r => r.Korisnik)
                .Where(r => r.Mec.KreatorID == korisnikID && r.Status == MecRequestStatus.Ceka)
                .OrderByDescending(r => r.DatumKreiranja)
                .ToListAsync();
        }
    }
}
