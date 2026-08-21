// Tasks.Application/Abstractions/IDomainEventDispatcher.cs
using Tasks.Domain.common;

namespace Tasks.Application.Abstractions;

/// <summary>
/// DESPACHANTE DE EVENTOS DE DOMÍNIO.
///
/// POR QUE EXISTE?
/// O agregado gera eventos (TaskCreatedEvent, TaskConcludedEvent...)
/// mas não sabe QUEM vai reagir. Esse abstrai quem vai publicar.
///
/// Fluxo:
/// 1. Handler executa caso de uso
/// 2. UoW faz commit
/// 3. Handler coleta DomainEvents do agregado
/// 4. Dispatcher publica um por um (notificar, log, integrar, etc)
/// 5. Agregado limpa a lista de eventos
///
/// POR QUE É INTERFACE?
/// Implementação real (MediatR, Service Bus, log simples) fica
/// na Infrastructure. Aqui só o contrato.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent @event, CancellationToken ct = default);
}
