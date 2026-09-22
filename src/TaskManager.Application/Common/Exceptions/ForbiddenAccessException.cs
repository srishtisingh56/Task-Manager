// Common/Exceptions/ForbiddenAccessException.cs
namespace TaskManager.Application.Common.Exceptions;

// Thrown for caller-aware authorization failures 
// Distinct from DomainException, which is entity-self-invariant only. The API-layer
// middleware will map DomainException -> 400 and this -> 403.
public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message) : base(message) { }
}