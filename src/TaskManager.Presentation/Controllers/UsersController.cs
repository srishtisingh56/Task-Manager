using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Users.Commands;
using TaskManager.Application.Users.Commands.AssignManager;
using TaskManager.Application.Users.Commands.CreateUser;
using TaskManager.Application.Users.Commands.DemoteFromSystemAdmin;
using TaskManager.Application.Users.Commands.PromoteToSystemAdmin;
using TaskManager.Application.Users.Commands.ReactivateUser;
using TaskManager.Application.Users.Commands.RemoveManager;
using TaskManager.Application.Users.Commands.UpdateContactDetails;
using TaskManager.Application.Users.Queries.GetAllUsers;
using TaskManager.Application.Users.Queries.GetMyDirectWorkers;
using TaskManager.Application.Users.Queries.GetUserById;

namespace TaskManager.Presentation.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController(ISender mediator) : ControllerBase
    {
        //WORKS
        [HttpPost]
        public async Task<IActionResult> Create(CreateUserCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllUsersQuery query, CancellationToken ct)
            => Ok(await mediator.Send(query, ct));

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
            => Ok(await mediator.Send(new GetUserByIdQuery(id), ct));

        [HttpGet("me/workers")]
        public async Task<IActionResult> MyWorkers(CancellationToken ct)
            => Ok(await mediator.Send(new GetMyDirectWorkersQuery(), ct));

        [HttpPatch("{id:guid}/contact")]
        public async Task<IActionResult> UpdateContact(Guid id, UpdateContactDetailsCommand body, CancellationToken ct)
        { await mediator.Send(new UpdateContactDetailsCommand(id,body.Name, body.Email, body.PhoneNumber), ct); return NoContent(); }

        //WORKS
        [HttpPut("{id:guid}/manager")]  
        public async Task<IActionResult> AssignManager(Guid id, AssignManagerCommand body, CancellationToken ct)
        {
            var result = await mediator.Send(new AssignManagerCommand(id, body.ManagerId), ct);
            return Ok(result);
        }

        [HttpDelete("{id:guid}/manager")]
        public async Task<IActionResult> RemoveManager(Guid id, CancellationToken ct)
        { await mediator.Send(new RemoveManagerCommand(id), ct); return NoContent(); }

        [HttpPost("{id:guid}/promote")]
        public async Task<IActionResult> Promote(Guid id, CancellationToken ct)
        { await mediator.Send(new PromoteToSystemAdminCommand(id), ct); return NoContent(); }

        [HttpPost("{id:guid}/demote")]
        public async Task<IActionResult> Demote(Guid id, CancellationToken ct)
        { await mediator.Send(new DemoteFromSystemAdminCommand(id), ct); return NoContent(); }

        [HttpPost("{id:guid}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        { await mediator.Send(new DeactivateUserCommand(id), ct); return NoContent(); }

        [HttpPost("{id:guid}/reactivate")]
        public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
        { await mediator.Send(new ReactivateUserCommand(id), ct); return NoContent(); }
    }
}
