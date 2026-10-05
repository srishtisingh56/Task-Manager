using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.NotificationRules.Commands.CreateNotificationRule;
using TaskManager.Application.NotificationRules.Queries.GetNotificationRulesForTask;
using TaskManager.Application.NotificationRules.Queries.GetNotificationRuleById;
using TaskManager.Application.NotificationRules.Commands.UpdateSchedule;
using TaskManager.Application.NotificationRules.Commands.DeleteNotificationRule;
using MediatR;

namespace TaskManager.Presentation.Controllers
{
    [ApiController]
    [Route("api/")]
    public class NotificationRulesController(ISender mediator) : ControllerBase
    {
        [HttpPost("tasks/{taskId:guid}/notification-rules")]
        public async Task<IActionResult> Create(Guid taskId, CreateNotificationRuleCommand body, CancellationToken ct)
        {
            var result = await mediator.Send(body with { TaskId = taskId }, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet("tasks/{taskId:guid}/notification-rules")]
        public async Task<IActionResult> GetForTask(Guid taskId, [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10, CancellationToken ct = default)
            => Ok(await mediator.Send(new GetNotificationRulesForTaskQuery(taskId, pageNumber, pageSize), ct));

        [HttpGet("notification-rules/{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
            => Ok(await mediator.Send(new GetNotificationRuleByIdQuery(id), ct));

        [HttpPatch("notification-rules/{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdateScheduleCommand body, CancellationToken ct)
        { await mediator.Send(body with { RuleId = id }, ct); return NoContent(); }

        [HttpDelete("notification-rules/{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        { await mediator.Send(new DeleteNotificationRuleCommand(id), ct); return NoContent(); }
    }

    //Get notifications for the current user
    
    // [ApiController]
    // [Route("api/notifications")]
    // public class NotificationsController(ISender mediator) : ControllerBase
    // {
    //     [HttpGet("me")]
    //     public async Task<IActionResult> Mine([FromQuery] GetMyNotificationsQuery query, CancellationToken ct)
    //         => Ok(await mediator.Send(query, ct));
    // }
}