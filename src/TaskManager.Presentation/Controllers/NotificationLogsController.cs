using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.NotificationLogs.Queries.GetNotificationLogs;

namespace TaskManager.Presentation.Controllers
{
    [ApiController]
    [Route("api/notification-logs")]
    public class NotificationLogsController(ISender mediator): ControllerBase
    {
        [HttpGet()]
        public async Task<IActionResult> Logs([FromQuery] GetNotificationLogsQuery query, CancellationToken ct)
            => Ok(await mediator.Send(query, ct));
    }
}