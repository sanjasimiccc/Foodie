using Entiteti;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;
using KeyHelper;
using Microsoft.AspNetCore.Authorization;
//using Newtonsoft.Json;
//using RedisRestorani.Hubs;

namespace RedisRestorani.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HranaController : ControllerBase
    {
        private readonly IDatabase _redisDb;
        private readonly GlobalCounter gCounter;
        //private readonly RestoranHub _restoranHub;


        private readonly IConnectionMultiplexer _mux; //dodato zbog pub/sub

        private readonly string globalCounterKey = "next.food.id";
        public HranaController(IConnectionMultiplexer mux) //, RestoranHub restoranHub
        {
            _mux = mux;
            //_restoranHub = restoranHub;
            _redisDb = mux.GetDatabase();
            gCounter = new GlobalCounter(mux);
            if (!gCounter.CheckNextGlobalCounterExists(globalCounterKey))
            {
                _redisDb.StringSet(globalCounterKey, "0");
            }
        }

        [HttpPost("dodajHranu"), Authorize(Roles = "admin")]
        public async Task<IActionResult> DodajHranu([FromBody] Hrana hrana)
        {
            if (hrana == null)
            {
                return BadRequest("Objekat hrane nije validan.");
            }

            try
            {
                string generatedId = gCounter.GetNextId(globalCounterKey);
                Console.WriteLine(generatedId);

                //hrana mora da pripada nekom restoranu, pa zato: 
                bool restoranPostoji = await _redisDb.KeyExistsAsync($"restoran:{hrana.RestoranId}:id");
                if (!restoranPostoji)
                {
                    return BadRequest("Restoran kome zelite da dodelite hranu ne postoji.");
                }
                await _redisDb.HashSetAsync($"hrana:{generatedId}:id",
                [
                    new HashEntry("Id", $"hrana:{generatedId}:id"), //mozda ne treba?
                    new HashEntry("Naziv", hrana.Naziv ?? string.Empty),
                    new HashEntry("Cena", hrana.Cena.ToString("F2")), // Konverzija double sa 2 decimale u string
                    new HashEntry("Opis", hrana.Opis ?? string.Empty),
                    new HashEntry("Slika", hrana.Slika ?? string.Empty),
                    new HashEntry("Kategorija", hrana.Kategorija ?? string.Empty),
                    new HashEntry("RestoranId", hrana.RestoranId ?? string.Empty), //"veza ka restoranu kome pripada"
                    new HashEntry("Kolicina", hrana.Kolicina.ToString()),
                ]);

                //dodati i u set ovog restorana koji pamti svu njegovu hranu!
                //i da set ne postoji, redis bi ga kreirao prilikom dodavanja prvog elementa!
                await _redisDb.SetAddAsync($"restaurant:{hrana.RestoranId}:foods", $"hrana:{generatedId}:id");

                // Dodajte ID u globalni SET za praćenje ID-jeva hrane
                await _redisDb.SetAddAsync("all_food_ids", $"hrana:{generatedId}:id");

                return Ok("Hrana restorana je uspešno dodata.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Došlo je do greške: {ex.Message}");
            }
        }

        [HttpGet("preuzmiHranu/{id}")]
        public async Task<IActionResult> PreuzmiHranu(string id) //ovo je samo generatedID, tako cemo?
        {
            //preuzmi
            var hashEntries = await _redisDb.HashGetAllAsync($"hrana:{id}:id");
            // Proveri da li postoji hrana
            if (hashEntries == null || hashEntries.Length == 0)
            {
                return NotFound("Hrana nije pronađena.");
            }

            // Mapiraj HashEntry vrednosti u Hrana objekat
            var hrana = new Hrana
            {
                IdHrane = hashEntries.FirstOrDefault(e => e.Name == "Id").Value,
                Naziv = hashEntries.FirstOrDefault(e => e.Name == "Naziv").Value.ToString(),
                Cena = Convert.ToDouble(hashEntries.FirstOrDefault(e => e.Name == "Cena").Value),
                Opis = hashEntries.FirstOrDefault(e => e.Name == "Opis").Value.ToString(),
                Slika = hashEntries.FirstOrDefault(e => e.Name == "Slika").Value.ToString(),
                Kategorija = hashEntries.FirstOrDefault(e => e.Name == "Kategorija").Value.ToString(),
                RestoranId = hashEntries.FirstOrDefault(e => e.Name == "RestoranId").Value.ToString(), //samo onaj srednji generisani, zbog brisanja posle
                Kolicina = Convert.ToInt32(hashEntries.FirstOrDefault(e => e.Name == "Kolicina").Value),
            };

            //chat kaze da je ovo bolje jer FirstOrDefaultAsync svaki put skenira ceo Hash da bi pronasao element, pa je ovo optimalnije
            //NISAM ISPROBALA
            // var hrana = new Hrana();
            // foreach (var entry in hashEntries)
            // {
            //     switch (entry.Name)
            //     {
            //         case "Id":
            //             hrana.IdHrane = entry.Value;
            //             break;
            //         case "Naziv":
            //             hrana.Naziv = entry.Value.ToString();
            //             break;
            //         case "Cena":
            //             hrana.Cena = Convert.ToDouble(entry.Value); // Pretpostavi "0" ako cena nije prisutna
            //             break;
            //         case "Opis":
            //             hrana.Opis = entry.Value.ToString();
            //             break;
            //         case "Slika":
            //             hrana.Slika = entry.Value.ToString();
            //             break;
            //     }
            // }

            return Ok(hrana);
        }

        [HttpDelete("obrisiHranu/{id}"), Authorize(Roles = "admin")]
        public async Task<IActionResult> ObrisiHranu(string id)
        {
            var hashEntries = await _redisDb.HashGetAllAsync($"hrana:{id}:id");
            // Proveri da li postoji hrana
            if (hashEntries == null || hashEntries.Length == 0)
            {
                return NotFound("Hrana nije pronađena.");
            }

            string idRestorana = hashEntries.FirstOrDefault(e => e.Name == "RestoranId").Value.ToString();

            // Uklanjanje ID hrane iz seta restorana
            await _redisDb.SetRemoveAsync($"restaurant:{idRestorana}:foods", $"hrana:{id}:id");

            var deleted = await _redisDb.KeyDeleteAsync($"hrana:{id}:id");
            if (!deleted)
            {
                return NotFound("Hrana nije pronađena za brisanje.");
            }


            // Uklonite ID hrane iz globalnog SET-a
            await _redisDb.SetRemoveAsync("all_food_ids", $"hrana:{id}:id");

            return Ok("Hrana uspešno obrisana.");
        }

        [HttpPut("azurirajHranu/{idHrane}/{restoranId}"), Authorize(Roles = "admin")]
        public async Task<IActionResult> AzurirajHranu(string idHrane, string restoranId, [FromBody] HranaDTO hrana)
        {
            if (hrana == null)
            {
                return BadRequest("Podaci za ažuriranje nisu validni.");
            }

            try
            {
                bool hranaPostoji = await _redisDb.KeyExistsAsync($"hrana:{idHrane}:id");
                if (!hranaPostoji)
                {
                    return NotFound("Hrana sa datim ID-jem nije pronađena.");
                }

                //--------------------
                // Dobijamo staru cenu sa Redis-a
                var existingCena = await _redisDb.HashGetAsync($"hrana:{idHrane}:id", "Cena");
                double oldPrice = double.Parse(existingCena!);
                Console.WriteLine(oldPrice);
                //--------------------

                var hashEntries = new List<HashEntry>();

                if (!string.IsNullOrEmpty(hrana.Naziv))
                {
                    hashEntries.Add(new HashEntry("Naziv", hrana.Naziv));
                }
                if (!string.IsNullOrEmpty(hrana.Cena.ToString()))
                {
                    hashEntries.Add(new HashEntry("Cena", hrana.Cena.ToString()));
                }
                if (!string.IsNullOrEmpty(hrana.Opis))
                {
                    hashEntries.Add(new HashEntry("Opis", hrana.Opis));
                }
                if (!string.IsNullOrEmpty(hrana.Slika))
                {
                    hashEntries.Add(new HashEntry("Slika", hrana.Slika));
                }
                if (!string.IsNullOrEmpty(hrana.Kategorija))
                {
                    hashEntries.Add(new HashEntry("Kategorija", hrana.Kategorija));
                }
                if (!string.IsNullOrEmpty(hrana.Kolicina.ToString()))
                {
                    hashEntries.Add(new HashEntry("Kolicina", hrana.Kolicina.ToString()));
                }

                // Proveriti da li postoje unosi za ažuriranje
                if (hashEntries.Count > 0)
                {
                    await _redisDb.HashSetAsync($"hrana:{idHrane}:id", [.. hashEntries]);
                    
                    //-----------------------------------
                    // Ako je cena smanjena, objavi obaveštenje
                    if (hrana.Cena < oldPrice)
                    {
                        Console.WriteLine("Smanjena je cena!");
                        var message = $"Cena hrane '{hrana.Naziv}' je smanjena! Nova cena: {hrana.Cena}";
                        var subscriber = _mux.GetSubscriber();
                        Console.WriteLine(message);
                        await subscriber.PublishAsync($"restoran:{restoranId}:akcije",  message);  // Objavi poruku na kanal
                        Console.WriteLine($"poruka objavljena na kanal: restoran:{restoranId}:akcije");
                    
                        // // Pozivamo PublishDiscount metodu umesto direktnog pozivanja PublishAsync
                        // await _restoranHub.PublishDiscount(restoranId, message); // Objavi poruku na kanal
                        // Console.WriteLine($"Poruka objavljena na kanal: restoran:{restoranId}:akcije");
                    }
                    //-----------------------------------
                    
                    return Ok("Hrana je uspešno ažurirana.");
                }
                else
                {
                    return BadRequest("Nema validnih polja za ažuriranje.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Došlo je do greške: {ex.Message}");
            }
        }

        [HttpGet("preuzmiSvuHranu")]
        public async Task<IActionResult> PreuzmiSvuHranu()
        {
            try
            {
                // // Dobijanje svih ključeva koji predstavljaju hranu
                // var rezultat = (RedisResult[])await _redisDb.ExecuteAsync("KEYS", "hrana:*:id");
                // var kljuceviHrane = rezultat.Select(r => (RedisKey)(string)r).ToArray();

                // Dohvati sve ID-jeve hrane iz globalnog SET-a
                var kljuceviHrane = await _redisDb.SetMembersAsync("all_food_ids");

                if (kljuceviHrane == null || kljuceviHrane.Length == 0)
                {
                    return NotFound("Nema hrane u bazi.");
                }

                var sveHrane = new List<Hrana>();

                foreach (var kljuc in kljuceviHrane)
                {
                    // Dohvatanje svih polja iz Hash-a za svaki ključ
                    var hashEntries = await _redisDb.HashGetAllAsync(kljuc.ToString());

                    var hrana = new Hrana
                    {
                        IdHrane = kljuc.ToString().Split(":")[1],
                        Naziv = hashEntries.FirstOrDefault(e => e.Name == "Naziv").Value.ToString(),
                        Cena = Convert.ToDouble(hashEntries.FirstOrDefault(e => e.Name == "Cena").Value),
                        Opis = hashEntries.FirstOrDefault(e => e.Name == "Opis").Value.ToString(),
                        Slika = hashEntries.FirstOrDefault(e => e.Name == "Slika").Value.ToString(),
                        Kategorija = hashEntries.FirstOrDefault(e => e.Name == "Kategorija").Value.ToString(),
                        RestoranId = hashEntries.FirstOrDefault(e => e.Name == "RestoranId").Value.ToString(),
                        Kolicina = Convert.ToInt32(hashEntries.FirstOrDefault(e => e.Name == "Kolicina").Value),
                    };

                    sveHrane.Add(hrana);
                }

                return Ok(sveHrane);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Došlo je do greške: {ex.Message}");
            }
        }
    }
}