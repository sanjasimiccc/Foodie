
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.SignalR;

// namespace RedisRestorani.Controllers{
    
//     [ApiController]
//     [Route("api/[controller]")]
//     public class DiscountController : ControllerBase
//     {
//         private readonly IHubContext<RestoranHub> _hubContext;

//         public DiscountController(IHubContext<RestoranHub> hubContext)
//         {
//             _hubContext = hubContext;
//         }

//         [HttpPost("publish")]
//         public async Task<IActionResult> PublishDiscount(string message)  //[FromBody] DiscountRequest request
//         {
//             // Objavljivanje popusta prema svim klijentima
//             await _hubContext.Clients.All.SendAsync("ReceiveDiscount", message); //request.Message
//             return Ok();
//         }
//     }
// }
