// Tasks.Application/Tasks/Commands/AssignTask/AssignTaskCommand.cs
using Tasks.Domain.TaskAggregate;

namespace Tasks.Application.Tasks.Commands.AssignTask;

public sealed record AssignTaskCommand(TaskItemId TaskId, Guid AssigneeUserId);
