using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Tasks.Commands.CreateTask;
using TaskManager.Application.Tasks.Queries.GetTaskById;
using TaskManager.Application.Tasks.Commands;
using TaskManager.Application.Tasks.Queries.GetTasksForUser;
using TaskManager.Application.Tasks.Commands.UpdateTaskDetails;
using TaskManager.Application.Tasks.Commands.UpdateTaskDeadlines;
using TaskManager.Application.Tasks.Commands.SetRepetitiveTask;
using TaskManager.Application.Tasks.Commands.ReassignTask;
using TaskManager.Application.Tasks.Commands.MarkTaskCompleted;
using TaskManager.Application.Tasks.Commands.HaltTask;
using TaskManager.Application.Tasks.Commands.ResumeTask;
using TaskManager.Application.Tasks.Commands.RestoreTask;
using TaskManager.Application.Tasks.Commands.DeleteTasksByStatus;
using TaskManager.Application.Tasks.Commands.DeleteTaskCommand;
namespace TaskManager.Presentation.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    public class TasksController(ISender mediator) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create(CreateTaskCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
            => Ok(await mediator.Send(new GetTaskByIdQuery(id), ct));
            
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetTasksForUserQuery query, CancellationToken ct)
            => Ok(await mediator.Send(query, ct));

        [HttpPut("{id:guid}/details")]
        public async Task<IActionResult> UpdateDetails(Guid id, UpdateTaskDetailsCommand body, CancellationToken ct)
        { await mediator.Send(body with { TaskId = id }, ct); return NoContent(); }

        [HttpPut("{id:guid}/deadlines")]
        public async Task<IActionResult> UpdateDeadlines(Guid id, UpdateTaskDeadlinesCommand body, CancellationToken ct)
        { await mediator.Send(body with { TaskId = id }, ct); return NoContent(); }

        [HttpPut("{id:guid}/repetitive")]
        public async Task<IActionResult> SetRepetitive(Guid id, SetRepetitiveTaskCommand body, CancellationToken ct)
        { await mediator.Send(body with { TaskId = id }, ct); return NoContent(); }

        [HttpPut("{id:guid}/assignee")]
        public async Task<IActionResult> Reassign(Guid id, ReassignTaskCommand body, CancellationToken ct)
        { await mediator.Send(body with { TaskId = id }, ct); return NoContent(); }

        [HttpPost("{id:guid}/complete")]
        public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
        { await mediator.Send(new MarkTaskCompletedCommand(id), ct); return NoContent(); }

        [HttpPost("{id:guid}/halt")]
        public async Task<IActionResult> Halt(Guid id, CancellationToken ct)
        { await mediator.Send(new HaltTaskCommand(id), ct); return NoContent(); }

        [HttpPost("{id:guid}/resume")]
        public async Task<IActionResult> Resume(Guid id, CancellationToken ct)
        { await mediator.Send(new ResumeTaskCommand(id), ct); return NoContent(); }

        [HttpPost("{id:guid}/restore")]
        public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        { await mediator.Send(new RestoreTaskCommand(id), ct); return NoContent(); }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        { await mediator.Send(new DeleteTaskCommand(id), ct); return NoContent(); }

        [HttpPost("bulk-delete")]
        public async Task<IActionResult> BulkDelete(DeleteTasksByStatusCommand command, CancellationToken ct)
        { await mediator.Send(command, ct); return NoContent(); }
    }
}