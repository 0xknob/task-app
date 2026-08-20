// Tasks.Domain/TaskAggregate/ITaskRepository.cs
using Tasks.Domain.common;

namespace Tasks.Domain.TaskAggregate;

/// <summary>
/// CONTRATO de persistência do agregado TaskItem.
///
/// POR QUE INTERFACE E NÃO CLASSE?
/// - O DOMAIN não quer saber se é Cosmos DB, SQL Server, em memória,
///   arquivo, API externa. Ele só diz "me dá uma TaskItem por este ID".
/// - Inverter a dependência (Repository Pattern) deixa a gente trocar
///   a implementação sem mexer no Domain.
/// - Facilita TESTES: nos testes, você usa uma implementação in-memory.
///
/// Implementação real fica em Tasks.Infrastructure.
///
/// NOTA sobre "Task":
/// "Task" também é o nome de System.Threading.Tasks.Task (async).
/// Em escopos onde "Task" pode colidir, qualificamos explicitamente.
/// Em escopos onde não precisa, o using global do SDK já resolve.
/// </summary>
public interface ITaskRepository
{
    Task<Result<TaskItem>> GetByIdAsync(
        TaskItemId id,
        CancellationToken ct = default);

    Task<Result<TaskItem>> AddAsync(
        TaskItem task,
        CancellationToken ct = default);

    Task<UnitResult> UpdateAsync(
        TaskItem task,
        CancellationToken ct = default);

    Task<UnitResult> DeleteAsync(
        TaskItemId id,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<TaskItem>>> ListAsync(
        TaskStatus? status = null,
        Priority? priority = null,
        Guid? assigneeUserId = null,
        CancellationToken ct = default);
}
