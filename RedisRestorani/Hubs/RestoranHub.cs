using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using StackExchange.Redis;
namespace RedisRestorani.Hubs;
public class RestoranHub : Hub
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<RestoranHub> _hubContext;


    public RestoranHub(IConnectionMultiplexer redis, IHubContext<RestoranHub> hubContext)
    {
        _hubContext = hubContext;
        _redis = redis;
    }

    // // Pretplata na popuste za određeni restoran
    // public async Task SubscribeToRestaurant(string restoranId)
    // {
    //     await Groups.AddToGroupAsync(Context.ConnectionId, restoranId);
    //     var subscriber = _redis.GetSubscriber(); //pribavljanje "subscriber" objekta koji se koristi za preuzimanje i slanje poruka u Redis Pub/Sub sistemu
    //     await subscriber.SubscribeAsync($"restoran:{restoranId}:akcije", async (channel, message) =>  //klijent se pretplacuje na kanal restoran:id:akcije --> sve poruke vezane za popuste odredjenog restorana
    //     {
    //         /// Slanje poruke svim klijentima u SignalR grupi
    //         await _hubContext.Clients.Group(restoranId).SendAsync("ReceiveDiscount", message);
    //     });
    // }

    public async Task SubscribeToRestaurant(string restoranId)
    {
        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, restoranId);
            var subscriber = _redis.GetSubscriber();
            await subscriber.SubscribeAsync($"restoran:{restoranId}:akcije", async (channel, message) =>
            {
                try
                {
                    await _hubContext.Clients.Group(restoranId).SendAsync("ReceiveDiscount", message);
                }
                catch (Exception ex)
                {
                    // Logovanje ili obraditi grešku pri slanju poruke klijentima
                    Console.WriteLine($"Greška pri slanju poruke grupi: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            // Logovanje ili obraditi grešku pri dodavanju u grupu
            Console.WriteLine($"Greška pri pretplati: {ex.Message}");
        }
    }


    // // Objavljivanje popusta
    // public async Task PublishDiscount(string restoranId, string message)
    // {
    //     var subscriber = _redis.GetSubscriber();
    //     await subscriber.PublishAsync($"restoran:{restoranId}:akcije", message);
    //     // Poruka će biti prosleđena svim klijentima koji su se pretplatili na kanal restoran:{restoranId}:akcije
    // }

    
    public async Task PublishDiscount(string restoranId, string message)
    {
        var subscriber = _redis.GetSubscriber();
        var jsonMessage = JsonConvert.SerializeObject(new { message }); 
        await subscriber.PublishAsync($"restoran:{restoranId}:akcije", jsonMessage);
    }
}
