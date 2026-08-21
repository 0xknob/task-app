// Tasks.Application/Tasks/Queries/ListTasks/ListTasksQuery.cs
using Tasks.Domain.TaskAggregate;

// Alias pra resolver ambiguidade com System.Threading.Tasks.TaskStatus
using TaskStatus = Tasks.Domain.TaskAggregate.TaskStatus;

namespace Tasks.Application.Tasks.Queries.ListTasks;

public sealed record ListTasksQuery(
    TaskStatus? Status = null,
    Priority? Priority = null,
    Guid? AssigneeUserId = null);
