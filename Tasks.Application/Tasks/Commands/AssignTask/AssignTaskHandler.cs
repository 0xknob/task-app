// Tasks.Application/Tasks/Commands/AssignTask/AssignTaskHandler.cs
using Tasks.Application.Abstractions;
using Tasks.Domain.TaskAggregate;
using Tasks.Domain.common;

namespace Tasks.Application.Tasks.Commands.AssignTask;

public sealed class AssignTaskHandler
{
    private readonly ITaskRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;

    public AssignTaskHandler(
        ITaskRepository repository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    public async Task<UnitResult> HandleAsync(
        AssignTaskCommand command,
        CancellationToken ct = default)
    {
        var getResult = await _repository.GetByIdAsync(command.TaskId, ct);
        if (getResult.IsFailure)
            return Result.Fail(getResult.Error!);

        var task = getResult.Value!;

        var assignResult = task.AssignTo(command.AssigneeUserId);
        if (assignResult.IsFailure)
            return Result.Fail(assignResult.Error!);

        var updateResult = await _repository.UpdateAsync(task, ct);
        if (updateResult.IsFailure)
            return Result.Fail(updateResult.Error!);

        var commitResult = await _unitOfWork.SaveChangesAsync(ct);
        if (commitResult.IsFailure)
            return Result.Fail(commitResult.Error!);

        await DispatchEventsAsync(task, ct);
        task.ClearDomainEvents();

        return Result.Ok();
    }

    private async Task DispatchEventsAsync(TaskItem task, CancellationToken ct)
    {
        foreach (var @event in task.DomainEvents)
        {
            await _dispatcher.DispatchAsync(@event, ct);
        }
    }
}
