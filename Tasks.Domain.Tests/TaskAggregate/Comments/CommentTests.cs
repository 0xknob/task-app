// Tasks.Domain.Tests/TaskAggregate/Comments/CommentTests.cs
using Tasks.Domain.TaskAggregate.Comments;

namespace Tasks.Domain.Tests.TaskAggregate.Comments;

public class CommentTests
{
    [Fact]
    public void Create_ComDadosValidos_RetornaSucesso()
    {
        var authorId = Guid.NewGuid();

        var result = Comment.Create(authorId, "Ótima tarefa!");

        Assert.True(result.IsSuccess);
        Assert.Equal(authorId, result.Value!.AuthorUserId);
        Assert.Equal("Ótima tarefa!", result.Value.Content);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
    }

    [Fact]
    public void Create_ComAutorVazio_RetornaFalha()
    {
        var result = Comment.Create(Guid.Empty, "Conteúdo");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_ComConteudoVazio_RetornaFalha()
    {
        var result = Comment.Create(Guid.NewGuid(), string.Empty);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_ComConteudoNulo_RetornaFalha()
    {
        var result = Comment.Create(Guid.NewGuid(), null!);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_Com1001Caracteres_RetornaFalha()
    {
        var textoGigante = new string('a', 1001);

        var result = Comment.Create(Guid.NewGuid(), textoGigante);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_FazTrimDoConteudo()
    {
        var result = Comment.Create(Guid.NewGuid(), "  conteúdo  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("conteúdo", result.Value!.Content);
    }

    [Fact]
    public void Edit_ComConteudoValido_RetornaSucesso()
    {
        var comment = Comment.Create(Guid.NewGuid(), "Original").Value!;

        var result = comment.Edit("Editado");

        Assert.True(result.IsSuccess);
        Assert.Equal("Editado", comment.Content);
    }

    [Fact]
    public void Edit_ComConteudoVazio_RetornaFalha()
    {
        var comment = Comment.Create(Guid.NewGuid(), "Original").Value!;

        var result = comment.Edit("");

        Assert.True(result.IsFailure);
        Assert.Equal("Original", comment.Content); // não muda se falha
    }

    [Fact]
    public void Edit_ComMaisDe1000Caracteres_RetornaFalha()
    {
        var comment = Comment.Create(Guid.NewGuid(), "Original").Value!;
        var textoGigante = new string('a', 1001);

        var result = comment.Edit(textoGigante);

        Assert.True(result.IsFailure);
    }
}
