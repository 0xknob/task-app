// Tasks.Application/Tasks/Queries/GetTaskById/GetTaskByIdHandler.cs
using Tasks.Domain.TaskAggregate;
using Tasks.Domain.common;

namespace Tasks.Application.Tasks.Queries.GetTaskById;

/// <summary>
/// QUERY — só lê, não muda nada.
///
/// Por que ainda retorna Result<T>?
/// - Task não encontrada NÃO é exception, é um resultado esperado
/// - Caller decide o que fazer (404, mostrar vazio, etc)
/// </summary>
public sealed class GetTaskByIdHandler
{
    private readonly ITaskRepository _repository;

    public GetTaskByIdHandler(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TaskItem>> HandleAsync(
        GetTaskByIdQuery query,
        CancellationToken ct = default)
    {
        return await _repository.GetByIdAsync(query.TaskId, ct);
    }
}
