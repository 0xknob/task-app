// Tasks.Application/Tasks/Commands/AddComment/AddCommentCommand.cs
using Tasks.Domain.TaskAggregate;

namespace Tasks.Application.Tasks.Commands.AddComment;

public sealed record AddCommentCommand(
    TaskItemId TaskId,
    Guid AuthorUserId,
    string Content);
