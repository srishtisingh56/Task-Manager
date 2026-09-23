
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Users.Common;
using MediatR;
using TaskManager.Domain.Entities;
namespace TaskManager.Application.Users.Commands.RemoveManager
{
    public sealed class RemoveManagerCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<RemoveManagerCommand, UserDto>
{
    public async Task<UserDto> Handle(RemoveManagerCommand request, CancellationToken ct)
    {
        var worker = await db.Users.FindAsync([request.WorkerId], ct)
            ?? throw new NotFoundException(nameof(User), request.WorkerId);

        if (!currentUser.IsSystemAdmin)
        {
            throw new ForbiddenAccessException("Only a system admin can remove manager relationships.");
        }

        worker.RemoveManager();

        await db.SaveChangesAsync(ct);

        return UserDto.FromEntity(worker);
    }
}
}