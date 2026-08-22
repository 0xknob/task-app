using Microsoft.Extensions.Logging;
using Tasks.Application.Abstractions;
using Tasks.Domain.common;

namespace Tasks.Infrastructure.Events;

/// <summary>
/// DISPATCHER DE EVENTOS QUE APENAS LOGA.
///
/// O QUE FAZ?
/// - Recebe um IDomainEvent e loga no console (ou ILogger).
/// - Em produção, aqui entraria um MediatR ou Service Bus pra realmente
///   notificar outros sistemas. Mas a interface fica.
///
/// POR QUE LOGAR?
/// - Pra debug. Você vê no console toda vez que uma tarefa é criada,
///   concluída, etc.
/// - Pra mostrar que o sistema tá vivo. Evento apareceu = ação aconteceu.
///
/// DEPENDÊNCIA: ILogger&lt;T&gt; (Microsoft.Extensions.Logging.Abstractions)
/// - Injetado via DI. A API configura o provedor (console, arquivo, etc).
/// </summary>
public sealed class LoggingDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly ILogger<LoggingDomainEventDispatcher> _logger;

    public LoggingDomainEventDispatcher(ILogger<LoggingDomainEventDispatcher> logger)
    {
        _logger = logger;
    }

    public Task DispatchAsync(IDomainEvent @event, CancellationToken ct = default)
    {
        // Loga o tipo do evento (ex: "TaskConcludedEvent") e o horário.
        _logger.LogInformation(
            "[Domain Event] {EventType} @ {OccurredOn}",
            @event.GetType().Name,
            @event.OccurredOn);

        return Task.CompletedTask;
    }
}
