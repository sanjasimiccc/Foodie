using Entiteti;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;
using KeyHelper;
using Microsoft.AspNetCore.Authorization;

namespace RedisRestorani.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OcenaController : ControllerBase
    {
        private readonly IDatabase _redisDb;
        private readonly GlobalCounter gCounter;
        private readonly IUserService _userService;

         private readonly string globalCounterKey = "next.rating.id"; // Globalni ključ za restorane
        public OcenaController(IConnectionMultiplexer mux, IUserService userService)
        {
            _redisDb = mux.GetDatabase();
            gCounter= new GlobalCounter(mux);
            this._userService = userService;
            if (!gCounter.CheckNextGlobalCounterExists(globalCounterKey))
            {
                _redisDb.StringSet(globalCounterKey, "0");
            }
        }


        [HttpPost("DodajOcenu/{restoranId}/{vrednost}"), Authorize(Roles = "user" )]
        public async Task<IActionResult> DodajOcenu(string restoranId, double vrednost)
        {
            var emailKorisnika = _userService.GetUser(); //preuzimam iz tokena
            if(emailKorisnika == null)
            {
                return StatusCode(401, $"Došlo je do greške prilikom preuzimanja informacija iz tokena");
            }
                
            string? korisnikId = await _redisDb.HashGetAsync($"users.email.addresses", emailKorisnika);

            bool korisnikPostoji = await _redisDb.KeyExistsAsync($"korisnik:{korisnikId}:id");
            if (!korisnikPostoji)
            {
                 return BadRequest("Korisnik za taj id ne postoji");
            }

            bool restoranPostoji = await _redisDb.KeyExistsAsync($"restoran:{restoranId}:id");
            if (!restoranPostoji)
            {
                 return BadRequest("Restoran za taj id ne postoji");
            }

            try
            {
                // ključ za restoran i dodaj ocenu
                string oceneRestoranKey = $"restoran:{restoranId}:ocene"; // Ključ za restoran sa ocenama
                
                // Generisanje ID-a za novu ocenu
                string ocenaId = gCounter.GetNextId(globalCounterKey);

                // Svaka ocena se dodaje kao Hash entry sa ključem: ocenaId
                await _redisDb.HashSetAsync(oceneRestoranKey, 
                    new HashEntry[] 
                    {
                        new HashEntry($"ocena:{ocenaId}:vrednost", vrednost.ToString()),
                        new HashEntry($"ocena:{ocenaId}:korisnikId", korisnikId)
                    });

                // Preračunavanje prosečne ocene
                var sveOcene = await _redisDb.HashGetAllAsync(oceneRestoranKey);

                // Izdvajanje vrednosti ocena
                var vrednostiOcena = sveOcene
                    .Where(e => e.Name.ToString().Contains(":vrednost"))
                    .Select(e => double.Parse(e.Value!))
                     .ToList();

                double novaProsecnaOcena = vrednostiOcena.Average();

                // Ažuriranje prosečne ocene u Redis Hash za restoran
                string restoranKey = $"restoran:{restoranId}:id";
                await _redisDb.HashSetAsync(restoranKey, new HashEntry[]
                {
                    new HashEntry("ProsecnaOcena", novaProsecnaOcena.ToString())
                });

                    // Ažuriranje "score" u Redis Sorted Set
                await _redisDb.SortedSetAddAsync("restorani_po_oceni", restoranKey, novaProsecnaOcena);

                return Ok("Ocena uspešno dodata.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Došlo je do greške: {ex.Message}");
            }
        }


    }

}