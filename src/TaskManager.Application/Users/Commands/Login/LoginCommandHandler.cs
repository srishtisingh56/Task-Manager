using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Users.Common;

namespace TaskManager.Application.Users.Commands.Login
{
    public sealed class LoginCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator
    ) : IRequestHandler<LoginCommand, AuthResultDto>
    {
        public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken ct)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);

            // Same generic message for "no such user" and "wrong password" — avoids leaking which emails are registered.
            if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedException("Invalid email or password.");

            if (!user.IsActive)
                throw new UnauthorizedException("This account has been deactivated.");

            var token = jwtTokenGenerator.GenerateToken(user, out var expiresAtUtc);

            return new AuthResultDto(token, expiresAtUtc, UserDto.FromEntity(user));
        }
    }
}
