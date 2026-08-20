// Tasks.Domain/TaskAggregate/Assignee/TaskAssignee.cs
namespace Tasks.Domain.TaskAggregate.Assignee;

/// <summary>
/// Representa a atribuição de uma tarefa a um usuário.
///
/// Por que é Value Object e não Entity?
/// - Só carrega um UserId (Guid). Não tem ciclo de vida próprio.
/// - Se você trocar o assignee, o anterior simplesmente deixa de existir.
/// - Não tem ID próprio nem comportamento que justifique Entity.
///
/// Aqui também poderíamos ter um UserId tipado, mas pra manter o
/// exemplo didático usamos Guid direto.
///
/// Nota sobre o nome:
/// Optamos por TaskAssignee (em vez de "Assignee") para evitar colisão
/// com o nome do namespace Tasks.Domain.TaskAggregate.Assignee.
/// Quando o nome do tipo casa com o do namespace, o compilador fica
/// confuso. Nomes distintos resolvem o problema sem hacks.
/// </summary>
public sealed record TaskAssignee
{
    public Guid UserId { get; }
    public DateTime AssignedAt { get; }

    // Construtor PRIVADO — único jeito de criar TaskAssignee é via From().
    private TaskAssignee(Guid userId, DateTime assignedAt)
    {
        UserId = userId;
        AssignedAt = assignedAt;
    }

    /// <summary>
    /// Fábrica usada pela infraestrutura ou seed para reconstruir.
    /// </summary>
    public static TaskAssignee From(Guid userId, DateTime assignedAt)
        => new(userId, assignedAt);
}
