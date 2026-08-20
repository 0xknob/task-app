// Tasks.Domain.Tests/TaskAggregate/TaskItemEditTests.cs
using Tasks.Domain.TaskAggregate;
using Tasks.Domain.TaskAggregate.Events;

namespace Tasks.Domain.Tests.TaskAggregate;

/// <summary>
/// Testes de edição de título e adição de comentários.
/// </summary>
public class TaskItemEditTests
{
    [Fact]
    public void ChangeTitle_ComNovoTituloValido_Altera()
    {
        var task = TestData.CriarTaskValida("Antigo");

        var result = task.ChangeTitle("Novo título");

        Assert.True(result.IsSuccess);
        Assert.Equal("Novo título", task.Title.Value);
    }

    [Fact]
    public void ChangeTitle_QuandoConcluida_RetornaFalha()
    {
        var task = TestData.CriarTaskValida();
        task.Conclude();

        var result = task.ChangeTitle("Novo");

        Assert.True(result.IsFailure);
        Assert.Equal("Tarefa de teste", task.Title.Value); // não muda
    }

    [Fact]
    public void ChangeTitle_ComTituloVazio_RetornaFalha()
    {
        var task = TestData.CriarTaskValida();

        var result = task.ChangeTitle("");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void AddComment_ComDadosValidos_Adiciona()
    {
        var task = TestData.CriarTaskValida();
        var authorId = Guid.NewGuid();

        var result = task.AddComment(authorId, "Comentário teste");

        Assert.True(result.IsSuccess);
        Assert.Single(task.Comments);
    }

    [Fact]
    public void AddComment_DisparaCommentAddedEvent()
    {
        var task = TestData.CriarTaskValida();
        var authorId = Guid.NewGuid();

        task.AddComment(authorId, "Comentário");

        var commentEvent = task.DomainEvents.OfType<CommentAddedEvent>().FirstOrDefault();
        Assert.NotNull(commentEvent);
        Assert.Equal(authorId, commentEvent!.AuthorUserId);
    }

    [Fact]
    public void AddComment_ComAutorVazio_RetornaFalha()
    {
        var task = TestData.CriarTaskValida();

        var result = task.AddComment(Guid.Empty, "Conteúdo");

        Assert.True(result.IsFailure);
        Assert.Empty(task.Comments);
    }

    [Fact]
    public void AddComment_ComConteudoVazio_RetornaFalha()
    {
        var task = TestData.CriarTaskValida();

        var result = task.AddComment(Guid.NewGuid(), "");

        Assert.True(result.IsFailure);
        Assert.Empty(task.Comments);
    }

    [Fact]
    public void AddComment_Varios_Acumula()
    {
        var task = TestData.CriarTaskValida();
        var author = Guid.NewGuid();

        task.AddComment(author, "Primeiro");
        task.AddComment(author, "Segundo");
        task.AddComment(author, "Terceiro");

        Assert.Equal(3, task.Comments.Count);
    }

    [Fact]
    public void AddComment_PermiteAdicionarMesmoComTaskConcluida()
    {
        // Comentário é ok após concluído — não bloqueia.
        var task = TestData.CriarTaskValida();
        task.Conclude();

        var result = task.AddComment(Guid.NewGuid(), "Comentário pós-conclusão");

        Assert.True(result.IsSuccess);
    }
}
