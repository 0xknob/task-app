// Tasks.Application/Tasks/Commands/ConcludeTask/ConcludeTaskCommand.cs
using Tasks.Domain.TaskAggregate;

namespace Tasks.Application.Tasks.Commands.ConcludeTask;

public sealed record ConcludeTaskCommand(TaskItemId TaskId);
