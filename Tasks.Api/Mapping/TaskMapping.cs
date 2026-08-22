// Tasks.Api/Mapping/TaskMapping.cs
using Tasks.Api.Responses;
using Tasks.Domain.TaskAggregate;

namespace Tasks.Api.Mapping;

/// <summary>
/// Mapeia TaskItem (Domain) → TaskResponse (DTO).
///
/// POR QUE EXISTE?
/// - Mantém API desacoplada do Domain
/// - Se Domain mudar (renomear campo, etc), só mexe aqui
/// - Conversões de enum pra string ficam centralizadas
/// </summary>
public static class TaskMapping
{
    public static TaskResponse ToResponse(this TaskItem task)
    {
        return new TaskResponse(
            Id: task.Id.Value,
            Title: task.Title.Value,
            Description: task.Description.Value,
            Priority: task.Priority.ToString(),
            Status: task.Status.ToString(),
            DueDate: task.DueDate.Value,
            CreatedAt: task.CreatedAt,
            ConcludedAt: task.ConcludedAt,
            Assignee: task.Assignee is null
                ? null
                : new AssigneeResponse(task.Assignee.UserId, task.Assignee.AssignedAt),
            Comments: task.Comments
                .Select(c => new CommentResponse(
                    c.Id, c.AuthorUserId, c.Content, c.CreatedAt))
                .ToList());
    }
}