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
    public Guid AuthorUserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // Construtor vazio privado: necessário pra EF Core /serialização no futuro.
    private Comment() { }

    // Construtor interno: Comment só nasce dentro de TaskItem.
    // Quem usar Result<Comment> na criação fica mais alinhado com DDD.
    internal static Result<Comment> Create(Guid authorUserId, string content)
    {
        if (authorUserId == Guid.Empty)
            return Result.Fail<Comment>("Autor do comentário é obrigatório.");

        if (string.IsNullOrWhiteSpace(content))
            return Result.Fail<Comment>("Conteúdo do comentário é obrigatório.");

        return Result.Ok(new Comment(authorUserId, content.Trim()));
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
        if (string.IsNullOrWhiteSpace(newContent))
            return Result.Fail("Conteúdo do comentário não pode ser vazio.");

        if (newContent.Length > 1000)
            return Result.Fail("Comentário não pode ter mais de 1000 caracteres.");

        Content = newContent.Trim();
        return Result.Ok();
    }
}
