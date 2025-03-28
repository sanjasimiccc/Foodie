using Entiteti;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using KeyHelper;
using Microsoft.AspNetCore.Authorization;

namespace RedisRestorani.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RestoraniController : ControllerBase
    {
        private readonly IDatabase _redisDb;
        private readonly GlobalCounter gCounter;

        private readonly string globalCounterKey = "next.restaurant.id"; // Globalni ključ za restorane
        public RestoraniController(IConnectionMultiplexer mux)
        {
            _redisDb = mux.GetDatabase();
            gCounter = new GlobalCounter(mux);
            if (!gCounter.CheckNextGlobalCounterExists(globalCounterKey))
            {
                _redisDb.StringSet(globalCounterKey, "0");
            }
        }

        [HttpPost("dodajRestoran"), Authorize(Roles = "admin" )]
        public async Task<IActionResult> DodajRestoran([FromBody] Restoran restoran)
        {
            if (restoran == null)
            {
                return BadRequest("Restoran nije validan.");
            }

            try
            {
                string generatedId = gCounter.GetNextId(globalCounterKey);
                // Kreiranje Redis hash zapisa
                await _redisDb.HashSetAsync($"restoran:{generatedId}:id", new HashEntry[]
                {
                    new HashEntry("Id", $"restoran:{generatedId}:id"), //mozda ne treba?
                    new HashEntry("Naziv", restoran.Naziv ?? string.Empty),
                    new HashEntry("Adresa", restoran.Adresa ?? string.Empty),
                    new HashEntry("Telefon", restoran.Telefon ?? string.Empty),
                    new HashEntry("Opis", restoran.Opis ?? string.Empty),
                    new HashEntry("Slika", restoran.Slika ?? string.Empty),
                    new HashEntry("ProsecnaOcena", restoran.ProsecnaOcena?.ToString() ?? "0"), //d0dala
                  //  new HashEntry("HranaId", string.Empty), //dodala

                });
               
                await _redisDb.SortedSetAddAsync("restorani_po_oceni", $"restoran:{generatedId}:id", restoran.ProsecnaOcena ?? 0);

                return Ok("Restoran uspešno dodat.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Došlo je do greške: {ex.Message}");
            }
        }
        [HttpGet("preuzmiRestoran/{idRestoran}")]
        public async Task<IActionResult> PreuzmiRestoran(string idRestoran)
        {
            // Dohvati sve hash unose za dati restoran
            var hashEntries = await _redisDb.HashGetAllAsync($"restoran:{idRestoran}:id");

            // Proveri da li postoji restoran
            if (hashEntries == null || hashEntries.Length == 0)
            {
                return NotFound("Restoran nije pronađen.");
            }

            // Mapiraj HashEntry vrednosti u Restoran objekat
            var restoran = new Restoran
            {
                //IdRestorana = int.Parse(hashEntries.FirstOrDefault(e => e.Name == "Id").Value.ToString()),
                IdRestorana = hashEntries.FirstOrDefault(e => e.Name == "Id").Value,
                Naziv = hashEntries.FirstOrDefault(e => e.Name == "Naziv").Value.ToString(), //dodale ovo da ne zuti zbog null
                Adresa = hashEntries.FirstOrDefault(e => e.Name == "Adresa").Value.ToString(),
                Telefon = hashEntries.FirstOrDefault(e => e.Name == "Telefon").Value.ToString(),
                Opis = hashEntries.FirstOrDefault(e => e.Name == "Opis").Value.ToString(),
                Slika = hashEntries.FirstOrDefault(e => e.Name == "Slika").Value.ToString(),
                ProsecnaOcena = double.TryParse(hashEntries.FirstOrDefault(e => e.Name == "ProsecnaOcena").Value, out var ocena) 
                        ? Math.Round(ocena, 2)
                        : (double?)null
            };

            return Ok(restoran);
        }


        [HttpDelete("obrisiRestoran/{idRestoran}"), Authorize(Roles = "admin" )]
        public async Task<IActionResult> ObrisiRestoran(string idRestoran)
        {
            var deleted = await _redisDb.KeyDeleteAsync($"restoran:{idRestoran}:id");
            if (!deleted)
            {
                return NotFound("Restoran nije pronađen za brisanje.");
            }
            //DODALA, skapirala kad sam isprobala brisanje, PROVERITIII!!!!
            var hranaIds = await _redisDb.SetMembersAsync($"restaurant:{idRestoran}:foods"); //tu su skladisteni celi kljucevi
            foreach (var hranaId in hranaIds)
            {
                await _redisDb.KeyDeleteAsync(hranaId.ToString());
                await _redisDb.SetRemoveAsync("all_food_ids", hranaId.ToString()); //i iz ovog seta sve hrane brisem
            }
            await _redisDb.SortedSetRemoveAsync("restorani_po_oceni", $"restoran:{idRestoran}:id"); //i iz ovog seta sve hrane brisem
            // Optionally, delete the set itself if it's no longer needed
            await _redisDb.KeyDeleteAsync($"restaurant:{idRestoran}:foods");

            
            return Ok("Restoran uspešno obrisan.");
        }


        [HttpPut("azuriraj/{idRestorana}"), Authorize(Roles = "admin" )]
        public async Task<IActionResult> AzurirajRestoran(string idRestorana, [FromBody] RestoranDTO restoranDto)
        {
            if (restoranDto == null)
            {
                return BadRequest("Restoran nije validan");
            }
            try
            {
                string restoranKey = $"restoran:{idRestorana}:id";

                if (!await _redisDb.KeyExistsAsync(restoranKey))
                {
                    return NotFound($"Restoran sa ID-em {idRestorana} ne postoji.");
                }
                var hashEntries = await _redisDb.HashGetAllAsync(restoranKey);
                if (hashEntries.Length == 0)
                {
                    return NotFound("Restoran nije pronađen.");
                }

                var hashUpdates = new List<HashEntry>();
                if (!string.IsNullOrEmpty(restoranDto.Naziv))
                {
                    hashUpdates.Add(new HashEntry("Naziv", restoranDto.Naziv));
                }
                if (!string.IsNullOrEmpty(restoranDto.Adresa))
                {
                    hashUpdates.Add(new HashEntry("Adresa", restoranDto.Adresa));
                }
                if (!string.IsNullOrEmpty(restoranDto.Telefon))
                {
                    hashUpdates.Add(new HashEntry("Telefon", restoranDto.Telefon));
                }
                if (!string.IsNullOrEmpty(restoranDto.Opis))
                {
                    hashUpdates.Add(new HashEntry("Opis", restoranDto.Opis));
                }
                if (!string.IsNullOrEmpty(restoranDto.Slika))
                {
                    hashUpdates.Add(new HashEntry("Slika", restoranDto.Slika));
                }
                if (hashUpdates.Count > 0)
                {
                    await _redisDb.HashSetAsync(restoranKey, hashUpdates.ToArray());
                }

                return Ok("Restoran je uspesno azuriran!");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Došlo je do greške: {ex.Message}");
            }
        }

        [HttpGet("preuzmiSveRestorane/{stranica}")]
        public async Task<IActionResult> PreuzmiSveRestorane(int stranica)
        {
            try
            {
                var keys = _redisDb.Multiplexer.GetServer(_redisDb.Multiplexer.GetEndPoints()[0]).Keys(pattern: "restoran:*:id");

                var restorani = new List<Restoran>();

                foreach (var key in keys)
                {
                    var hashEntries = await _redisDb.HashGetAllAsync(key);

                    var restoran = new Restoran
                    {
                        IdRestorana = key.ToString().Split(":")[1],
                        Naziv = hashEntries.FirstOrDefault(h => h.Name == "Naziv").Value.ToString(),
                        Adresa = hashEntries.FirstOrDefault(h => h.Name == "Adresa").Value.ToString(),
                        Telefon = hashEntries.FirstOrDefault(h => h.Name == "Telefon").Value.ToString(),
                        Opis = hashEntries.FirstOrDefault(h => h.Name == "Opis").Value.ToString(),
                        Slika = hashEntries.FirstOrDefault(h => h.Name == "Slika").Value.ToString(),
                        ProsecnaOcena = double.TryParse(hashEntries.FirstOrDefault(h => h.Name == "ProsecnaOcena").Value, out var ocena) 
                        ? Math.Round(ocena, 2) 
                        : (double?)null
                    };
                    restorani.Add(restoran);
                }

                int pocetniIndeks = (stranica - 1) * 5;
                int brojElemenata = 5;
                if(restorani.Count - (stranica - 1) * 5 < 5)
                {
                    brojElemenata = restorani.Count - (stranica - 1) * 5;
                }

                var restoraniStranice = restorani.GetRange(pocetniIndeks, brojElemenata);

                var restoraniZaPrikaz = new
                {
                    lista = restoraniStranice,
                    kraj = brojElemenata < 5
                };

                return Ok(restoraniZaPrikaz);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Došlo je do greške: {ex.Message}");
            }
        }

        [HttpGet("preuzmiHranuRestorana/{restoranId}/{stranica}")]
        public async Task<IActionResult> PreuzmiHranuRestorana(string restoranId, int stranica)
        {
            if(stranica < 1)
            {
                return BadRequest("Stranica mora da bude broj veci od 0");
            }

            try
            {
                bool restoranPostoji = await _redisDb.KeyExistsAsync($"restoran:{restoranId}:id");
                if (!restoranPostoji)
                {
                    return NotFound("Restoran nije pronađen.");
                }

                var hranaIds = await _redisDb.SetMembersAsync($"restaurant:{restoranId}:foods");
                var sveHrane = new List<Hrana>();

                if (hranaIds == null || hranaIds.Length == 0)
                {
                    return Ok(sveHrane);
                }

                foreach (var hranaId in hranaIds)
                {
                    var hashEntries = await _redisDb.HashGetAllAsync(hranaId.ToString());

                    var hrana = new Hrana
                    {
                        IdHrane = hranaId.ToString().Split(":")[1],
                        Naziv = hashEntries.FirstOrDefault(e => e.Name == "Naziv").Value.ToString(),
                        Cena = Convert.ToDouble(hashEntries.FirstOrDefault(e => e.Name == "Cena").Value),
                        Opis = hashEntries.FirstOrDefault(e => e.Name == "Opis").Value.ToString(),
                        Slika = hashEntries.FirstOrDefault(e => e.Name == "Slika").Value.ToString(),
                        Kategorija = hashEntries.FirstOrDefault(e => e.Name == "Kategorija").Value.ToString(),
                        RestoranId = hashEntries.FirstOrDefault(e => e.Name == "RestoranId").Value.ToString(),
                        Kolicina = Convert.ToInt32(hashEntries.FirstOrDefault(e => e.Name == "Kolicina").Value)
                       
                    };
                    sveHrane.Add(hrana);
                }

                int pocetniIndeks = (stranica - 1) * 5;
                int brojElemenata = 5;
                if(sveHrane.Count - (stranica - 1) * 5 < 5)
                {
                    brojElemenata = sveHrane.Count - (stranica - 1) * 5;
                }

                var hranaStranice = sveHrane.GetRange(pocetniIndeks, brojElemenata);

                var hranaZaPrikaz = new
                {
                    lista = hranaStranice,
                    kraj = brojElemenata < 5
                };

                return Ok(hranaZaPrikaz);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Došlo je do greške: {ex.Message}");
            }
        }

        [HttpGet("pretraziRestoranePoNazivu/{stranica}")]
        public async Task<IActionResult> PretraziRestoranePoNazivu([FromQuery] string naziv, int stranica) //da pitamo da li moze ovako ili ne
        {
            if (string.IsNullOrWhiteSpace(naziv))
            {
                return BadRequest("Naziv restorana ne može biti prazan.");
            }

            var restorani = new List<Restoran>();
            try
            {
                var server = _redisDb.Multiplexer.GetServer(_redisDb.Multiplexer.GetEndPoints().First());

                foreach (var key in server.Keys(pattern: "restoran:*:id"))
                {
                    var hashEntries = await _redisDb.HashGetAllAsync(key);

                    var restoranNaziv = hashEntries.FirstOrDefault(e => e.Name == "Naziv").Value.ToString();

                    if (restoranNaziv.Contains(naziv, StringComparison.OrdinalIgnoreCase))
                    {
                        var restoran = new Restoran
                        {
                            IdRestorana = hashEntries.FirstOrDefault(e => e.Name == "Id").Value,
                            Naziv = restoranNaziv,
                            Adresa = hashEntries.FirstOrDefault(e => e.Name == "Adresa").Value.ToString(),
                            Telefon = hashEntries.FirstOrDefault(e => e.Name == "Telefon").Value.ToString(),
                            Opis = hashEntries.FirstOrDefault(e => e.Name == "Opis").Value.ToString(),
                            Slika = hashEntries.FirstOrDefault(e => e.Name == "Slika").Value.ToString(),
                            ProsecnaOcena = double.TryParse(hashEntries.FirstOrDefault(h => h.Name == "ProsecnaOcena").Value, out var ocena) 
                            ? Math.Round(ocena, 2) 
                            : (double?)null
                        };

                        restorani.Add(restoran);
                    }
                }

                int pocetniIndeks = (stranica - 1) * 5;
                int brojElemenata = 5;
                if(restorani.Count - (stranica - 1) * 5 < 5)
                {
                    brojElemenata = restorani.Count - (stranica - 1) * 5;
                }

                var restoraniStranice = restorani.GetRange(pocetniIndeks, brojElemenata);

                var restoraniZaPrikaz = new
                {
                    lista = restoraniStranice,
                    kraj = brojElemenata < 5
                };

                return Ok(restoraniZaPrikaz);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Došlo je do greške: {ex.Message}");
            }
        }
    
        [HttpGet("Top10Restorana")]
        public async Task<IActionResult> GetTop10Restorana()
        {
            try
            {
                // Dohvatanje top 10 restorana iz Redis Sorted Set
                //var topRestoraniIds = await _redisDb.SortedSetRangeByScoreAsync("restorani_po_oceni", order: Order.Descending, start: 5, stop: 10);// ovo je u tom opsegu ocena mozda korisno ako imamo filtere
                var topRestoraniIds = await _redisDb.SortedSetRangeByRankAsync("restorani_po_oceni", 0, 9, Order.Descending);

                var restorani = new List<Restoran>();

                foreach (var restoranId in topRestoraniIds)
                {
                    var hashEntries = await _redisDb.HashGetAllAsync(restoranId.ToString());

                    var restoran = new Restoran
                    {
                        IdRestorana = restoranId.ToString().Split(":")[1],
                        Naziv = hashEntries.FirstOrDefault(h => h.Name == "Naziv").Value!,
                        Adresa = hashEntries.FirstOrDefault(h => h.Name == "Adresa").Value!,
                        Telefon = hashEntries.FirstOrDefault(h => h.Name == "Telefon").Value!,
                        Opis = hashEntries.FirstOrDefault(h => h.Name == "Opis").Value!,
                        Slika = hashEntries.FirstOrDefault(h => h.Name == "Slika").Value!,
                        //ProsecnaOcena = double.TryParse(hashEntries.FirstOrDefault(h => h.Name == "ProsecnaOcena").Value, out var ocena) ? ocena : null
                          ProsecnaOcena = double.TryParse(hashEntries.FirstOrDefault(h => h.Name == "ProsecnaOcena").Value, out var ocena) 
                        ? Math.Round(ocena, 2) 
                        : (double?)null
                    };

                    restorani.Add(restoran);
                }

                return Ok(restorani);
            }
            catch (Exception ex)
            {
                // Logujte grešku (opciono) i vratite status
                return StatusCode(500, $"Došlo je do greške: {ex.Message}");
            }
        }
    
    }
}