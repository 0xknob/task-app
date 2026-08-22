// Tasks.Api/Requests/CreateTaskRequest.cs
using System.ComponentModel.DataAnnotations;
using Tasks.Domain.TaskAggregate;

namespace Tasks.Api.Requests;

/// <summary>
/// DTO de entrada para criar uma tarefa.
///
/// POR QUE EXISTE?
/// - A API não aceita o Domain direto (senão ela teria que conhecer
///   Title, Description, etc como tipos separados).
/// - Recebe dados primitivos (string, DateTime, Priority).
/// - Validação automática via DataAnnotations ([Required], [StringLength]).
/// </summary>
public sealed record CreateTaskRequest
{
    [Required(ErrorMessage = "Título é obrigatório.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Título deve ter entre 1 e 200 caracteres.")]
    public string Title { get; init; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Descrição não pode ter mais de 2000 caracteres.")]
    public string Description { get; init; } = string.Empty;

    [Required]
    [EnumDataType(typeof(Priority), ErrorMessage = "Prioridade inválida.")]
    public Priority Priority { get; init; } = Priority.Medium;

    [Required(ErrorMessage = "Prazo é obrigatório.")]
    public DateTime DueDate { get; init; }
}