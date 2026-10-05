using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;

namespace TaskManager.Presentation.Services
{
    // Presentation/Services/CurrentUserService.cs
    public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
    {
        private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

      public Guid UserId
    {
        get
        {
            var value = Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(value, out var id)
                ? id
                : throw new UnauthorizedException("No authenticated user.");
        }
    }
        public bool IsSystemAdmin =>
            bool.TryParse(Principal?.FindFirst("is_admin")?.Value, out var isAdmin) && isAdmin;
    }
}