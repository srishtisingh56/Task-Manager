// using System;
// using System.Collections.Generic;
// using System.IdentityModel.Tokens.Jwt;
// using System.Linq;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.SignalR;

// namespace TaskManager.Presentation.Hubs
// {
//     [Authorize]
//     public sealed class NotificationHub : Hub
//     {
//         public static string GroupFor(Guid userId) => $"user:{userId}";

//         public override async Task OnConnectedAsync()
//         {
//             var sub = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
//             if (Guid.TryParse(sub, out var id))
//                 await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(id));
//             await base.OnConnectedAsync();
//         }
//     }
// }