using TaskManager.Domain.Entities;

namespace TaskManager.Application.Common.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user, out DateTime expiresAtUtc);
    }
}
