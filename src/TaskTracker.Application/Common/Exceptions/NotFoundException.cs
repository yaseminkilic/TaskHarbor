namespace TaskTracker.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"{entity} with key '{key}' was not found.")
    {
        Entity = entity;
        Key = key;
    }

    public string Entity { get; }
    public object Key { get; }
}
