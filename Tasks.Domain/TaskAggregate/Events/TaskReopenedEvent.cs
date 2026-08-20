using Tasks.Domain.common;
using Tasks.Domain.TaskAggregate;

namespace Tasks.Domain.TaskAggregate.Events;

public sealed record TaskReopenedEvent(
    TaskItemId TaskId,
    DateTime ReopenedAt,
    DateTime OccurredOn) : IDomainEvent;
