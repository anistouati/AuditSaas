namespace Shared.Messaging;

public record AuditScheduledEvent(Guid Id, string Title, DateTime ScheduledDate, string AssignedTo);
