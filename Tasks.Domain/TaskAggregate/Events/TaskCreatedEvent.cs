using Tasks.Domain.common;
using Tasks.Domain.TaskAggregate;

namespace Tasks.Domain.TaskAggregate.Events;

/// <summary>
/// Disparado quando uma NOVA tarefa é criada.
/// 
/// Convenção: 
/// - Nome no passado (Created, não Create)
/// - Carrega dados suficientes pra reconstruir o que aconteceu
/// - NÃO tem comportamento (é só dado)
/// </summary>
public sealed record TaskCreatedEvent(
    TaskItemId TaskId,
    string Title,
    Priority Priority,
    DateTime OccurredOn) : IDomainEvent;
