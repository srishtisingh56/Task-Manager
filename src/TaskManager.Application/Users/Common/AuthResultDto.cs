namespace TaskManager.Application.Users.Common
{
    public sealed record AuthResultDto(
        string Token,
        DateTime ExpiresAtUtc,
        UserDto User
    );
}
