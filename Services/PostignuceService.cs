using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Matchletic.Data;
using Matchletic.Models;

namespace Matchletic.Services
{
    public class PostignuceService
    {
        private readonly ApplicationDbContext _context;

        public PostignuceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ProvjeriPostignuca(int korisnikID)
        {
            // Provjeri dostignuće "Prvi Meč"
            await ProvjeriPrviMec(korisnikID);

            // Provjeri dostignuće "Niz Pobjeda"
            await ProvjeriNizPobjeda(korisnikID);

            // Provjeri dostignuće "Društveni Leptir"
            await ProvjeriDrustveniLeptir(korisnikID);

            // Provjeri dostignuće "Savršen Rezultat"
            await ProvjeriSavrsenRezultat(korisnikID);

            // Provjeri dostignuće "Pobjednik Turnira"
            await ProvjeriPobjednikTurnira(korisnikID);
        }

        private async Task ProvjeriPrviMec(int korisnikID)
        {
            var postignuce = await _context.Postignuca.FirstOrDefaultAsync(p => p.Naziv == "Prvi Meč");
            if (postignuce == null) return;

            var korisnikPostignuce = await _context.KorisnikPostignuca
                .FirstOrDefaultAsync(kp => kp.KorisnikID == korisnikID && kp.PostignuceID == postignuce.PostignuceID);

            var brojZavrsenihMeceva = await _context.MeceviKorisnici
                .CountAsync(mk => mk.KorisnikID == korisnikID && mk.Mec.Status == StatusMeca.Zavrsen);

            if (brojZavrsenihMeceva > 0)
            {
                if (korisnikPostignuce == null)
                {
                    korisnikPostignuce = new KorisnikPostignuce
                    {
                        KorisnikID = korisnikID,
                        PostignuceID = postignuce.PostignuceID,
                        NapredakProcenti = 100,
                        DatumOtkljucavanja = DateTime.Now
                    };
                    _context.KorisnikPostignuca.Add(korisnikPostignuce);
                    await _context.SaveChangesAsync();

                    // Ovdje možete dodati notifikaciju
                    await DodajNotifikacijuOtkljucavanja(korisnikID, postignuce.Naziv);
                }
                else if (korisnikPostignuce.NapredakProcenti < 100)
                {
                    korisnikPostignuce.NapredakProcenti = 100;
                    korisnikPostignuce.DatumOtkljucavanja = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Ovdje možete dodati notifikaciju
                    await DodajNotifikacijuOtkljucavanja(korisnikID, postignuce.Naziv);
                }
            }
        }

        private async Task ProvjeriNizPobjeda(int korisnikID)
        {
            var postignuce = await _context.Postignuca.FirstOrDefaultAsync(p => p.Naziv == "Niz Pobjeda");
            if (postignuce == null) return;

            var korisnikPostignuce = await _context.KorisnikPostignuca
                .FirstOrDefaultAsync(kp => kp.KorisnikID == korisnikID && kp.PostignuceID == postignuce.PostignuceID);

            // Dohvati završene mečeve korisnika, sortirano po datumu
            var zavrseniMecevi = await _context.Mecevi
                .Include(m => m.KorisniciMeca)
                .Where(m => m.Status == StatusMeca.Zavrsen &&
                      m.KorisniciMeca.Any(km => km.KorisnikID == korisnikID))
                .OrderByDescending(m => m.DatumMeca)
                .ToListAsync();

            int brojPobjeda = 0;

            foreach (var mec in zavrseniMecevi)
            {
                if (mec.Rezultat != null && mec.Rezultat.Contains($"Pobjednik: {korisnikID}"))
                {
                    brojPobjeda++;
                }
                else
                {
                    break; // Prekinut niz pobjeda
                }
            }

            int napredak = Math.Min(brojPobjeda * 20, 100); // 5 pobjeda = 100%
            bool otkljucano = napredak == 100;

            if (korisnikPostignuce == null)
            {
                korisnikPostignuce = new KorisnikPostignuce
                {
                    KorisnikID = korisnikID,
                    PostignuceID = postignuce.PostignuceID,
                    NapredakProcenti = napredak,
                    DatumOtkljucavanja = otkljucano ? DateTime.Now : null
                };
                _context.KorisnikPostignuca.Add(korisnikPostignuce);
                await _context.SaveChangesAsync();

                if (otkljucano)
                {
                    await DodajNotifikacijuOtkljucavanja(korisnikID, postignuce.Naziv);
                }
            }
            else if (korisnikPostignuce.NapredakProcenti != napredak)
            {
                bool biloOtkljucano = korisnikPostignuce.NapredakProcenti == 100;
                korisnikPostignuce.NapredakProcenti = napredak;

                if (otkljucano && !biloOtkljucano)
                {
                    korisnikPostignuce.DatumOtkljucavanja = DateTime.Now;
                    await DodajNotifikacijuOtkljucavanja(korisnikID, postignuce.Naziv);
                }

                await _context.SaveChangesAsync();
            }
        }

        // Slične metode za ostala dostignuća...

        private async Task DodajNotifikacijuOtkljucavanja(int korisnikID, string nazivDostignuca)
        {
            // Ovdje dodajte kod za kreiranje notifikacije o otključavanju dostignuća
            // Ako imate već implementirani NotifikacijaService, možete ga koristiti
            var notifikacija = new Notifikacija
            {
                KorisnikID = korisnikID,
                Naslov = "Novo dostignuće otključano!",
                Sadrzaj = $"Čestitamo! Otključali ste dostignuće: {nazivDostignuca}",
                Url = "/Postignuca",
                Tip = NotifikacijaTip.DostignuceOtkljucano,
                DatumKreiranja = DateTime.Now,
                Procitano = false
            };

            _context.Notifikacije.Add(notifikacija);
            await _context.SaveChangesAsync();
        }

        private async Task ProvjeriPobjednikTurnira(int korisnikID)
        {
            var postignuce = await _context.Postignuca.FirstOrDefaultAsync(p => p.Naziv == "Pobjednik Turnira");
            if (postignuce == null) return;

            var korisnikPostignuce = await _context.KorisnikPostignuca
                .FirstOrDefaultAsync(kp => kp.KorisnikID == korisnikID && kp.PostignuceID == postignuce.PostignuceID);

            // Trenutno turniri nisu implementirani, pa postavljamo napredak na 0
            // Kad se implementiraju turniri, ovo možemo zamijeniti stvarnom logikom
            int napredak = 0;
            bool otkljucano = false;

            // Ova logika će biti aktivirana kad se implementiraju turniri:
            // var osvojenTurnir = await _context.Mecevi
            //     .Where(m => m.Status == StatusMeca.Zavrsen &&
            //            m.JeTurnir && // Dodati JeTurnir svojstvo u Mec model kada bude potrebno
            //            m.KorisniciMeca.Any(km => km.KorisnikID == korisnikID) &&
            //            m.Rezultat.Contains($"Pobjednik: {korisnikID}"))
            //     .AnyAsync();
            // 
            // napredak = osvojenTurnir ? 100 : 0;
            // otkljucano = napredak == 100;

            if (korisnikPostignuce == null)
            {
                korisnikPostignuce = new KorisnikPostignuce
                {
                    KorisnikID = korisnikID,
                    PostignuceID = postignuce.PostignuceID,
                    NapredakProcenti = napredak,
                    DatumOtkljucavanja = otkljucano ? DateTime.Now : null
                };
                _context.KorisnikPostignuca.Add(korisnikPostignuce);
                await _context.SaveChangesAsync();

                if (otkljucano)
                {
                    await DodajNotifikacijuOtkljucavanja(korisnikID, postignuce.Naziv);
                }
            }
            else if (korisnikPostignuce.NapredakProcenti != napredak)
            {
                bool biloOtkljucano = korisnikPostignuce.NapredakProcenti == 100;
                korisnikPostignuce.NapredakProcenti = napredak;

                if (otkljucano && !biloOtkljucano)
                {
                    korisnikPostignuce.DatumOtkljucavanja = DateTime.Now;
                    await DodajNotifikacijuOtkljucavanja(korisnikID, postignuce.Naziv);
                }

                await _context.SaveChangesAsync();
            }
        }

        private async Task ProvjeriDrustveniLeptir(int korisnikID)
        {
            var postignuce = await _context.Postignuca.FirstOrDefaultAsync(p => p.Naziv == "Društveni Leptir");
            if (postignuce == null) return;

            var korisnikPostignuce = await _context.KorisnikPostignuca
                .FirstOrDefaultAsync(kp => kp.KorisnikID == korisnikID && kp.PostignuceID == postignuce.PostignuceID);

            var razlicitiIgraci = await _context.MeceviKorisnici
                .Where(mk => mk.Mec.KorisniciMeca.Any(k => k.KorisnikID == korisnikID) &&
                       mk.KorisnikID != korisnikID &&
                       mk.Mec.Status == StatusMeca.Zavrsen)
                .Select(mk => mk.KorisnikID)
                .Distinct()
                .CountAsync();

            int napredak = Math.Min(razlicitiIgraci * 10, 100); // 10 različitih igrača = 100%
            bool otkljucano = napredak == 100;

            if (korisnikPostignuce == null)
            {
                korisnikPostignuce = new KorisnikPostignuce
                {
                    KorisnikID = korisnikID,
                    PostignuceID = postignuce.PostignuceID,
                    NapredakProcenti = napredak,
                    DatumOtkljucavanja = otkljucano ? DateTime.Now : null
                };
                _context.KorisnikPostignuca.Add(korisnikPostignuce);
                await _context.SaveChangesAsync();

                if (otkljucano)
                {
                    await DodajNotifikacijuOtkljucavanja(korisnikID, postignuce.Naziv);
                }
            }
            else if (korisnikPostignuce.NapredakProcenti != napredak)
            {
                bool biloOtkljucano = korisnikPostignuce.NapredakProcenti == 100;
                korisnikPostignuce.NapredakProcenti = napredak;

                if (otkljucano && !biloOtkljucano)
                {
                    korisnikPostignuce.DatumOtkljucavanja = DateTime.Now;
                    await DodajNotifikacijuOtkljucavanja(korisnikID, postignuce.Naziv);
                }

                await _context.SaveChangesAsync();
            }
        }

        private async Task ProvjeriSavrsenRezultat(int korisnikID)
        {
            var postignuce = await _context.Postignuca.FirstOrDefaultAsync(p => p.Naziv == "Savršen Rezultat");
            if (postignuce == null) return;

            var korisnikPostignuce = await _context.KorisnikPostignuca
                .FirstOrDefaultAsync(kp => kp.KorisnikID == korisnikID && kp.PostignuceID == postignuce.PostignuceID);

            var imaSavrsenRezultat = await _context.Mecevi
                .Where(m => m.Status == StatusMeca.Zavrsen &&
                       m.KorisniciMeca.Any(km => km.KorisnikID == korisnikID) &&
                       m.Rezultat.Contains("Savršena pobjeda"))
                .AnyAsync();

            int napredak = imaSavrsenRezultat ? 100 : 0;
            bool otkljucano = napredak == 100;

            if (korisnikPostignuce == null)
            {
                korisnikPostignuce = new KorisnikPostignuce
                {
                    KorisnikID = korisnikID,
                    PostignuceID = postignuce.PostignuceID,
                    NapredakProcenti = napredak,
                    DatumOtkljucavanja = otkljucano ? DateTime.Now : null
                };
                _context.KorisnikPostignuca.Add(korisnikPostignuce);
                await _context.SaveChangesAsync();

                if (otkljucano)
                {
                    await DodajNotifikacijuOtkljucavanja(korisnikID, postignuce.Naziv);
                }
            }
            else if (korisnikPostignuce.NapredakProcenti != napredak)
            {
                bool biloOtkljucano = korisnikPostignuce.NapredakProcenti == 100;
                korisnikPostignuce.NapredakProcenti = napredak;

                if (otkljucano && !biloOtkljucano)
                {
                    korisnikPostignuce.DatumOtkljucavanja = DateTime.Now;
                    await DodajNotifikacijuOtkljucavanja(korisnikID, postignuce.Naziv);
                }

                await _context.SaveChangesAsync();
            }
        }




    }
}
