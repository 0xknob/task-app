// Tasks.Api/Responses/TaskResponse.cs
using Tasks.Domain.TaskAggregate;
using Tasks.Domain.TaskAggregate.Assignee;
using Tasks.Domain.TaskAggregate.Comments;

namespace Tasks.Api.Responses;

/// <summary>
/// DTO de saída — representa uma TaskItem pro mundo externo.
///
/// POR QUE NÃO RETORNAR O DOMAIN DIRETO?
/// - Domain tem invariantes internas que o mundo não precisa ver
/// - Acoplamento: API fica presa ao shape do Domain
/// - Controle de versão: mudar Domain não quebra clientes HTTP
/// </summary>
public sealed record TaskResponse(
    Guid Id,
    string Title,
    string Description,
    string Priority,
    string Status,
    DateTime DueDate,
    DateTime CreatedAt,
    DateTime? ConcludedAt,
    AssigneeResponse? Assignee,
    IReadOnlyList<CommentResponse> Comments);

public sealed record AssigneeResponse(Guid UserId, DateTime AssignedAt);

public sealed record CommentResponse(
    Guid Id,
    Guid AuthorUserId,
    string Content,
    DateTime CreatedAt);