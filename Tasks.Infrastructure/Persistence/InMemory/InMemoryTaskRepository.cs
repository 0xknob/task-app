using System.Collections.Concurrent;
using Tasks.Domain.TaskAggregate;
using Tasks.Domain.common;

// Alias pra resolver ambiguidade com System.Threading.Tasks.TaskStatus
using TaskStatus = Tasks.Domain.TaskAggregate.TaskStatus;

namespace Tasks.Infrastructure.Persistence.InMemory;

/// <summary>
/// REPOSITÓRIO IN-MEMORY.
///
/// O QUE É?
/// - Implementa ITaskRepository (do Domain) usando um Dictionary em memória.
/// - Roda sem banco, sem Docker, sem Cosmos, sem nada.
///
/// POR QUE EXISTE?
/// - Útil pra testes: roda em milissegundos, sem dependência externa.
/// - Útil pra desenvolvimento local: você codifica a API/Front sem ter
///   Cosmos configurado. Plug no InMemory, trabalha, depois troca.
///
/// COMO FUNCIONA?
/// - ConcurrentDictionary: thread-safe. Se duas requisições tentarem
///   adicionar ao mesmo tempo, não dá ruim.
/// - CopyOnWrite: devolvemos uma CÓPIA da lista, não a original. Assim
///   o caller não mexe no estado interno do repository.
/// </summary>
public sealed class InMemoryTaskRepository : ITaskRepository
{
    // Dictionary thread-safe. Chave = TaskId.Value (Guid).
    private readonly ConcurrentDictionary<Guid, TaskItem> _tasks = new();

    public Task<Result<TaskItem>> GetByIdAsync(TaskItemId id, CancellationToken ct = default)
    {
        if (_tasks.TryGetValue(id.Value, out var task))
            return Task.FromResult(Result.Ok(task));

        return Task.FromResult(Result.Fail<TaskItem>($"Tarefa {id.Value} não encontrada."));
    }

    public Task<Result<TaskItem>> AddAsync(TaskItem task, CancellationToken ct = default)
    {
        // Em Cosmos, a chave de partição é TaskId. Aqui é a mesma coisa.
        var added = _tasks.TryAdd(task.Id.Value, task);
        if (!added)
            return Task.FromResult(Result.Fail<TaskItem>($"Tarefa {task.Id.Value} já existe."));

        return Task.FromResult(Result.Ok(task));
    }

    public Task<UnitResult> UpdateAsync(TaskItem task, CancellationToken ct = default)
    {
        // AddOrUpdate: se já existe, sobrescreve. Se não, adiciona.
        _tasks.AddOrUpdate(task.Id.Value, task, (_, _) => task);
        return Task.FromResult(Result.Ok());
    }

    public Task<UnitResult> DeleteAsync(TaskItemId id, CancellationToken ct = default)
    {
        var removed = _tasks.TryRemove(id.Value, out _);
        if (!removed)
            return Task.FromResult(Result.Fail($"Tarefa {id.Value} não encontrada."));

        return Task.FromResult(Result.Ok());
    }

    public Task<Result<IReadOnlyList<TaskItem>>> ListAsync(
        TaskStatus? status = null,
        Priority? priority = null,
        Guid? assigneeUserId = null,
        CancellationToken ct = default)
    {
        // Pega todos e aplica filtros na memória.
        // Em Cosmos, cada filtro vira uma cláusula WHERE na query.
        IEnumerable<TaskItem> query = _tasks.Values;

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (priority.HasValue)
            query = query.Where(t => t.Priority == priority.Value);

        if (assigneeUserId.HasValue)
            query = query.Where(t => t.Assignee?.UserId == assigneeUserId.Value);

        // Materializa a lista. ToList() executa a query.
        var lista = query.ToList();

        return Task.FromResult(Result.Ok<IReadOnlyList<TaskItem>>(lista));
    }
}
