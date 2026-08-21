// Tasks.Application/Tasks/Queries/GetTaskById/GetTaskByIdQuery.cs
using Tasks.Domain.TaskAggregate;

namespace Tasks.Application.Tasks.Queries.GetTaskById;

public sealed record GetTaskByIdQuery(TaskItemId TaskId);
