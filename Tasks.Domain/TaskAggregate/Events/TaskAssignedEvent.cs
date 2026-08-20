using Tasks.Domain.common;
using Tasks.Domain.TaskAggregate;

namespace Tasks.Domain.TaskAggregate.Events;

public sealed record TaskAssignedEvent(
    TaskItemId TaskId,
    Guid AssigneeUserId,
    DateTime AssignedAt,
    DateTime OccurredOn) : IDomainEvent;
