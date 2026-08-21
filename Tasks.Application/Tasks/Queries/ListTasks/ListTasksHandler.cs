// Tasks.Application/Tasks/Queries/ListTasks/ListTasksHandler.cs
using Tasks.Domain.TaskAggregate;
using Tasks.Domain.common;

namespace Tasks.Application.Tasks.Queries.ListTasks;

public sealed class ListTasksHandler
{
    private readonly ITaskRepository _repository;

    public ListTasksHandler(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<TaskItem>>> HandleAsync(
        ListTasksQuery query,
        CancellationToken ct = default)
    {
        return await _repository.ListAsync(
            query.Status,
            query.Priority,
            query.AssigneeUserId,
            ct);
    }
}
