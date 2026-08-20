// Tasks.Domain/TaskAggregate/Comments/Comment.cs
using Tasks.Domain.common;

namespace Tasks.Domain.TaskAggregate.Comments;

/// <summary>
/// Comentário dentro de uma tarefa. É uma ENTIDADE (não VO) porque:
/// - Tem identidade (CommentId)
/// - Tem ciclo de vida (nasce quando adicionado, pode ser editado/removido)
/// - Tem comportamento próprio (curtir, editar conteúdo)
///
/// MAS é uma entidade INTERNA do agregado TaskItem.
/// Isso significa:
/// - Não tem repositório próprio
/// - Só é acessível via TaskItem
/// - Não existe "fora" do agregado
/// </summary>
public sealed class Comment : Entity<Guid>
{
    public const int MaxContentLength = 1000;

    public Guid AuthorUserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // Construtor vazio privado: necessário pra EF Core /serialização no futuro.
    private Comment() { }

    // Construtor interno: Comment só nasce dentro de TaskItem.
    // Quem usar Result<Comment> na criação fica mais alinhado com DDD.
    internal static Result<Comment> Create(Guid authorUserId, string content)
    {
        var validation = Validate(authorUserId, content);
        if (validation.IsFailure)
            return Result.Fail<Comment>(validation.Error!);

        return Result.Ok(new Comment(authorUserId, content.Trim()));
    }

    private static UnitResult Validate(Guid authorUserId, string content)
    {
        if (authorUserId == Guid.Empty)
            return Result.Fail("Autor do comentário é obrigatório.");

        if (string.IsNullOrWhiteSpace(content))
            return Result.Fail("Conteúdo do comentário é obrigatório.");

        if (content.Length > MaxContentLength)
            return Result.Fail($"Comentário não pode ter mais de {MaxContentLength} caracteres.");

        return Result.Ok();
    }

    private Comment(Guid authorUserId, string content)
    {
        Id = Guid.NewGuid();
        AuthorUserId = authorUserId;
        Content = content;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Editar conteúdo do comentário.
    /// </summary>
    public UnitResult Edit(string newContent)
    {
        var validation = Validate(AuthorUserId, newContent);
        if (validation.IsFailure)
            return Result.Fail(validation.Error!);

        Content = newContent.Trim();
        return Result.Ok();
    }
}
