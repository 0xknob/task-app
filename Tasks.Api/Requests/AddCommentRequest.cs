// Tasks.Api/Requests/AddCommentRequest.cs
using System.ComponentModel.DataAnnotations;
using Tasks.Domain.TaskAggregate.Comments;

namespace Tasks.Api.Requests;

public sealed record AddCommentRequest
{
    [Required(ErrorMessage = "Autor é obrigatório.")]
    public Guid AuthorUserId { get; init; }

    [Required(ErrorMessage = "Conteúdo é obrigatório.")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "Conteúdo deve ter entre 1 e 1000 caracteres.")]
    public string Content { get; init; } = string.Empty;
}