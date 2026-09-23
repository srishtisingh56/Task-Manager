

namespace TaskManager.Application.Users.Common.Interfaces
{
    public interface IManagerHierarchyService
    {
        Task<bool> WouldCreateCycleAsync(Guid workerId, Guid proposedManagerId, CancellationToken ct);
    }
}