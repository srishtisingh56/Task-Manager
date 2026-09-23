using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Users.Common;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Users.Commands.UpdateContactDetails
{
    public sealed class UpdateContactDetailsCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser
    ) : IRequestHandler<UpdateContactDetailsCommand, UserDto>
    {
        public async Task<UserDto> Handle(UpdateContactDetailsCommand request, CancellationToken ct)
        {
            var user = await db.Users.FindAsync([request.UserId], ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

            if(user.Id != currentUser.UserId && !currentUser.IsSystemAdmin)
            {
                throw new ForbiddenAccessException("You can only update your own contact details");
            }
             
            var name = request.Name.GetValueOrExisting(user.Name);
            var email = request.Email.GetValueOrExisting(user.Email);
            var phoneNumber = request.PhoneNumber.GetValueOrExisting(user.PhoneNumber);

            user.UpdateContactDetails(name, email, phoneNumber);

            await db.SaveChangesAsync(ct);

            return UserDto.FromEntity(user);
        }
    }
}