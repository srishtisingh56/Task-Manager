using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Tasks.Commands.CreateTask;
using TaskManager.Application.Tasks.Queries.GetTaskById;

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
    }
}