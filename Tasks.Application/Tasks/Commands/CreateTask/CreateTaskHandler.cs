// Tasks.Application/Tasks/Commands/CreateTask/CreateTaskHandler.cs
using Tasks.Application.Abstractions;
using Tasks.Domain.TaskAggregate;
using Tasks.Domain.common;

namespace Tasks.Application.Tasks.Commands.CreateTask;

/// <summary>
/// HANDLER — executa o caso de uso "criar tarefa".
///
/// RESPONSABILIDADES:
/// 1. Chamar o Domain.Create (validações de negócio)
/// 2. Persistir via Repository
/// 3. Commitar via UnitOfWork
/// 4. Despachar eventos de domínio
/// 5. Limpar eventos do agregado
/// 6. Devolver resultado
///
/// NÃO TEM:
/// - Validações de negócio (Domain cuida)
/// - Lógica de HTTP (API cuida)
/// - Lógica de banco (Infrastructure cuida)
/// </summary>
public sealed class CreateTaskHandler
{
    private readonly ITaskRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;

    public CreateTaskHandler(
        ITaskRepository repository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task<Result<CreateTaskResult>> HandleAsync(
        CreateTaskCommand command,
        CancellationToken ct = default)
    {
        // 1. Domain cria a entidade (valida tudo).
        var createResult = TaskItem.Create(
            command.Title,
            command.Description,
            command.Priority,
            command.DueDate);

        if (createResult.IsFailure)
            return Result.Fail<CreateTaskResult>(createResult.Error!);

        var task = createResult.Value!;

        // 2. Persiste no banco.
        var addResult = await _repository.AddAsync(task, ct);
        if (addResult.IsFailure)
            return Result.Fail<CreateTaskResult>(addResult.Error!);

        // 3. Commit transacional.
        var commitResult = await _unitOfWork.SaveChangesAsync(ct);
        if (commitResult.IsFailure)
            return Result.Fail<CreateTaskResult>(commitResult.Error!);

        // 4. Despacha eventos (após commit — garante consistência).
        await DispatchEventsAsync(task, ct);

        // 5. Limpa eventos pra não reprocessar.
        task.ClearDomainEvents();

        // 6. Retorna DTO de sucesso.
        return Result.Ok(new CreateTaskResult(task.Id));
    }

    private async Task DispatchEventsAsync(TaskItem task, CancellationToken ct)
    {
        foreach (var @event in task.DomainEvents)
        {
            await _dispatcher.DispatchAsync(@event, ct);
        }
    }
}
