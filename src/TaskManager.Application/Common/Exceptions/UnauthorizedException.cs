namespace TaskManager.Application.Common.Exceptions;

// Thrown for failed authentication (bad credentials) — API layer maps this to 401.
public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
