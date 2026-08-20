// Tasks.Domain.Tests/TaskAggregate/TaskItemCreateTests.cs
using Tasks.Domain.TaskAggregate;
using Tasks.Domain.TaskAggregate.Events;

// Alias pra resolver ambiguidade com System.Threading.Tasks.TaskStatus
using TaskStatus = Tasks.Domain.TaskAggregate.TaskStatus;

namespace Tasks.Domain.Tests.TaskAggregate;

/// <summary>
/// Testes do factory Create(). Cobrem validação e estado inicial.
/// </summary>
public class TaskItemCreateTests
{
    [Fact]
    public void Create_ComDadosValidos_RetornaSucesso()
    {
        var result = TaskItem.Create(
            "Título",
            "Descrição",
            Priority.High,
            DateTime.UtcNow.AddDays(3));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public void Create_TarefaIniciaComStatusPending()
    {
        var task = TestData.CriarTaskValida();

        Assert.Equal(TaskStatus.Pending, task.Status);
    }

    [Fact]
    public void Create_TarefaIniciaSemAssignee()
    {
        var task = TestData.CriarTaskValida();

        Assert.Null(task.Assignee);
    }

    [Fact]
    public void Create_TarefaIniciaSemComentarios()
    {
        var task = TestData.CriarTaskValida();

        Assert.Empty(task.Comments);
    }

    [Fact]
    public void Create_TarefaIniciaSemDataConclusao()
    {
        var task = TestData.CriarTaskValida();

        Assert.Null(task.ConcludedAt);
    }

    [Fact]
    public void Create_AtribuiIdValido()
    {
        var task = TestData.CriarTaskValida();

        Assert.NotEqual(Guid.Empty, task.Id.Value);
    }

    [Fact]
    public void Create_DisparaTaskCreatedEvent()
    {
        var task = TestData.CriarTaskValida();

        var createdEvent = task.DomainEvents.OfType<TaskCreatedEvent>().FirstOrDefault();
        Assert.NotNull(createdEvent);
        Assert.Equal(task.Id, createdEvent!.TaskId);
        Assert.Equal(task.Title.Value, createdEvent.Title);
        Assert.Equal(task.Priority, createdEvent.Priority);
    }

    [Fact]
    public void Create_ComTituloVazio_RetornaFalha()
    {
        var result = TaskItem.Create(
            "",
            "Descrição",
            Priority.Medium,
            DateTime.UtcNow.AddDays(3));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_ComDescricaoMuitoLonga_RetornaFalha()
    {
        var descGigante = new string('a', Description.MaxLength + 1);

        var result = TaskItem.Create(
            "Título",
            descGigante,
            Priority.Medium,
            DateTime.UtcNow.AddDays(3));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_ComPrazoNoPassado_RetornaFalha()
    {
        var result = TaskItem.Create(
            "Título",
            "Descrição",
            Priority.Medium,
            DateTime.UtcNow.AddDays(-7));

        Assert.True(result.IsFailure);
    }
}
