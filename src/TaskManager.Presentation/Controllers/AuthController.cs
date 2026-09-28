using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Users.Commands.Login;
using TaskManager.Application.Users.Queries.GetUserById;

namespace TaskManager.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(ISender mediator,  ICurrentUserService currentUser) : ControllerBase
    {
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginCommand command, CancellationToken ct)
        {
            var result = await mediator.Send(command, ct);
            return Ok(result);
        }

         [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
        => Ok(await mediator.Send(new GetUserByIdQuery(currentUser.UserId), ct));
    }
}
