using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Matchletic.Data;
using Matchletic.Models;

namespace Matchletic.Controllers
{
    [Authorize]
    public class PostignucaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PostignucaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Postignuca
        public async Task<IActionResult> Index()
        {
            // Dobiti trenutno prijavljenog korisnika
            var korisnikID = HttpContext.Session.GetInt32("KorisnikID");
            if (korisnikID == null)
            {
                return Challenge();
            }

            // Dohvati sva dostignuća
            var svaDostignuca = await _context.Postignuca.ToListAsync();

            // Ako nema dostignuća u bazi, seeduj ih odmah
            if (!svaDostignuca.Any())
            {
                await SeedDostignucaAsync();
                // Dohvati ponovno nakon seed-anja
                svaDostignuca = await _context.Postignuca.ToListAsync();
            }

            // Dohvati korisnikova dostignuća
            var korisnikPostignuca = await _context.KorisnikPostignuca
                .Where(kp => kp.KorisnikID == korisnikID)
                .ToListAsync();

            // Dohvati potrebne podatke za izračun napretka
            var brojZavrsenihMeceva = await _context.MeceviKorisnici
                .Where(mk => mk.KorisnikID == korisnikID && mk.Mec.Status == StatusMeca.Zavrsen)
                .CountAsync();

            var brojUzastopnihPobjeda = await IzracunajUzastopnePobjede(korisnikID.Value);

            var razlicitiIgraci = await _context.MeceviKorisnici
                .Where(mk => mk.Mec.KorisniciMeca.Any(k => k.KorisnikID == korisnikID) &&
                       mk.KorisnikID != korisnikID &&
                       mk.Mec.Status == StatusMeca.Zavrsen)
                .Select(mk => mk.KorisnikID)
                .Distinct()
                .CountAsync();

            var imaSavrsenRezultat = await _context.Mecevi
                .Where(m => m.Status == StatusMeca.Zavrsen &&
                       m.KorisniciMeca.Any(km => km.KorisnikID == korisnikID) &&
                       m.Rezultat.Contains("Savršena pobjeda"))
                .AnyAsync();

            // Budući da trenutno ne implementirate turnire, postavite vrijednost na false
            var osvojenTurnir = false;

            // Lista za prikaz u view-u
            var rezultatZaPrikaz = new List<KorisnikPostignuce>();

            // Obradi svako dostignuće i izračunaj napredak
            foreach (var dostignuce in svaDostignuca)
            {
                // Provjeri ima li korisnik već zapis o ovom dostignuću
                var korisnikDostignuce = korisnikPostignuca
                    .FirstOrDefault(kp => kp.PostignuceID == dostignuce.PostignuceID);

                // Ako nema, kreiramo privremeni objekt za prikaz
                if (korisnikDostignuce == null)
                {
                    korisnikDostignuce = new KorisnikPostignuce
                    {
                        KorisnikID = korisnikID.Value,
                        PostignuceID = dostignuce.PostignuceID,
                        NapredakProcenti = 0,
                        DatumOtkljucavanja = null
                    };
                }

                // Postavimo referencu na Postignuce (za prikaz u viewu)
                korisnikDostignuce.Postignuce = dostignuce;

                int napredak = 0;
                bool otključano = false;
                DateTime? datumOtkljucavanja = null;

                // Izračunaj napredak za svako dostignuće
                switch (dostignuce.Naziv)
                {
                    case "Prvi Meč":
                        napredak = brojZavrsenihMeceva > 0 ? 100 : 0;
                        otključano = napredak == 100;
                        datumOtkljucavanja = otključano ? (korisnikDostignuce.DatumOtkljucavanja ?? DateTime.Now) : null;
                        break;
                    case "Niz Pobjeda":
                        napredak = Math.Min(brojUzastopnihPobjeda * 20, 100); // 5 pobjeda = 100%
                        otključano = napredak == 100;
                        datumOtkljucavanja = otključano ? (korisnikDostignuce.DatumOtkljucavanja ?? DateTime.Now) : null;
                        break;
                    case "Društveni Leptir":
                        napredak = Math.Min(razlicitiIgraci * 10, 100); // 10 različitih igrača = 100%
                        otključano = napredak == 100;
                        datumOtkljucavanja = otključano ? (korisnikDostignuce.DatumOtkljucavanja ?? DateTime.Now) : null;
                        break;
                    case "Savršen Rezultat":
                        napredak = imaSavrsenRezultat ? 100 : 0;
                        otključano = napredak == 100;
                        datumOtkljucavanja = otključano ? (korisnikDostignuce.DatumOtkljucavanja ?? DateTime.Now) : null;
                        break;
                    case "Pobjednik Turnira":
                        napredak = osvojenTurnir ? 100 : 0;
                        otključano = napredak == 100;
                        datumOtkljucavanja = otključano ? (korisnikDostignuce.DatumOtkljucavanja ?? DateTime.Now) : null;
                        break;
                    default:
                        napredak = korisnikDostignuce?.NapredakProcenti ?? 0;
                        otključano = napredak == 100;
                        datumOtkljucavanja = korisnikDostignuce?.DatumOtkljucavanja;
                        break;
                }

                // Ažuriraj napredak za prikaz u viewu
                korisnikDostignuce.NapredakProcenti = napredak;
                korisnikDostignuce.DatumOtkljucavanja = datumOtkljucavanja;

                // Dodaj u listu za prikaz
                rezultatZaPrikaz.Add(korisnikDostignuce);

                // Ažuriraj ili kreiraj zapis u bazi
                var postojeciZapis = await _context.KorisnikPostignuca
                    .FirstOrDefaultAsync(kp => kp.KorisnikID == korisnikID && kp.PostignuceID == dostignuce.PostignuceID);

                if (postojeciZapis == null)
                {
                    // Kreiraj novi zapis
                    _context.KorisnikPostignuca.Add(new KorisnikPostignuce
                    {
                        KorisnikID = korisnikID.Value,
                        PostignuceID = dostignuce.PostignuceID,
                        NapredakProcenti = napredak,
                        DatumOtkljucavanja = datumOtkljucavanja
                    });
                }
                else if (postojeciZapis.NapredakProcenti != napredak ||
                         (datumOtkljucavanja != null && postojeciZapis.DatumOtkljucavanja == null))
                {
                    // Ažuriraj postojeći zapis
                    postojeciZapis.NapredakProcenti = napredak;
                    postojeciZapis.DatumOtkljucavanja = datumOtkljucavanja;
                    _context.KorisnikPostignuca.Update(postojeciZapis);
                }
            }

            // Sačuvaj promjene u bazi
            await _context.SaveChangesAsync();

            // Provjeri sadrži li lista dostignuća i podatke o dostignućima
            var praviRezultat = rezultatZaPrikaz.All(kp => kp.Postignuce != null);
            if (!praviRezultat)
            {
                // Ako nemamo reference na Postignuce, moramo ih dohvatiti posebno
                var svaPostignucaDict = svaDostignuca.ToDictionary(p => p.PostignuceID, p => p);
                foreach (var kp in rezultatZaPrikaz)
                {
                    if (kp.Postignuce == null && svaPostignucaDict.TryGetValue(kp.PostignuceID, out var p))
                    {
                        kp.Postignuce = p;
                    }
                }
            }

            // Prosljeđujemo listu svih dostignuća s korisnikovim napretkom
            return View(rezultatZaPrikaz);
        }

        private async Task<int> IzracunajUzastopnePobjede(int korisnikID)
        {
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

            return brojPobjeda;
        }

        private async Task SeedDostignucaAsync()
        {
            var dostignuca = new List<Postignuce>
    {
        new Postignuce
        {
            Naziv = "Prvi Meč",
            Opis = "Završite vaš prvi meč",
            Ikona = "🏆", // Dodano polje Ikona
            IkonaTip = "trophy",
            BojaKlasa = "bg-yellow-100 text-yellow-600"
        },
        new Postignuce
        {
            Naziv = "Niz Pobjeda",
            Opis = "Pobjedite u 5 mečeva zaredom",
            Ikona = "⚡", // Dodano polje Ikona
            IkonaTip = "zap",
            BojaKlasa = "bg-blue-100 text-blue-600"
        },
        new Postignuce
        {
            Naziv = "Društveni Leptir",
            Opis = "Igrajte sa 10 različitih igrača",
            Ikona = "👥", // Dodano polje Ikona
            IkonaTip = "users",
            BojaKlasa = "bg-purple-100 text-purple-600"
        },
        new Postignuce
        {
            Naziv = "Savršen Rezultat",
            Opis = "Pobjedite u meču bez gubitka poena",
            Ikona = "✅", // Dodano polje Ikona
            IkonaTip = "check-circle",
            BojaKlasa = "bg-green-100 text-green-600"
        },
        new Postignuce
        {
            Naziv = "Pobjednik Turnira",
            Opis = "Osvojite turnir",
            Ikona = "🏆", // Dodano polje Ikona
            IkonaTip = "trophy",
            BojaKlasa = "bg-gray-100 text-gray-600"
        },
        new Postignuce
        {
            Naziv = "Majstor Izazova",
            Opis = "Ispunite sve dnevne izazove za sedmicu",
            Ikona = "📊", // Dodano polje Ikona
            IkonaTip = "bar-chart-2",
            BojaKlasa = "bg-orange-100 text-orange-600"
        }
    };

            _context.Postignuca.AddRange(dostignuca);
            await _context.SaveChangesAsync();
        }

    }
}

