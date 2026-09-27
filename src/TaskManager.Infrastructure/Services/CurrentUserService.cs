using System;
using TaskManager.Application.Common.Interfaces;

namespace TaskManager.Infrastructure.Services
{
    // Stand-in until the API layer exists to resolve identity from HttpContext/JWT claims.
    public class CurrentUserService : ICurrentUserService
    {
        public Guid UserId { get; } 
        public bool IsSystemAdmin { get; }
    }
}
