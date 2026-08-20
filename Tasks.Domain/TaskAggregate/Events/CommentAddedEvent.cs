using Tasks.Domain.common;
using Tasks.Domain.TaskAggregate;

namespace Tasks.Domain.TaskAggregate.Events;

public sealed record CommentAddedEvent(
    TaskItemId TaskId,
    Guid CommentId,
    Guid AuthorUserId,
    DateTime OccurredOn) : IDomainEvent;
