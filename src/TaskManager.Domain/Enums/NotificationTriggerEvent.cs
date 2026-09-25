namespace TaskManager.Domain.Enums
{
    public enum NotificationTriggerEvent
    {
        Created = 0,
        Updated = 1,
        Deleted = 2,
        Completed = 3,
        AfterCreationOffset = 4,
        BeforeLenientDeadline = 5,
        BeforeStrictDeadline = 6,
        StrictDeadlinePassed = 7
    }
}