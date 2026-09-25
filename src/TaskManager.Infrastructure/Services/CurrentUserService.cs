using System;
using TaskManager.Application.Common.Interfaces;

namespace TaskManager.Infrastructure.Services
{
    // Stand-in until the API layer exists to resolve identity from HttpContext/JWT claims.
    public class CurrentUserService : ICurrentUserService
    {
        public Guid UserId { get; } = Guid.Parse("00000000-0000-0000-0000-000000000001");
        public bool IsSystemAdmin { get; } = true;
    }
}
