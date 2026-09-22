namespace TaskManager.Application.Common.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"\"{entityName}\" with Id:{key}was not found.") { }
}