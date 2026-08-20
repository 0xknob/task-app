// Tasks.Domain.Tests/TaskAggregate/TaskItemStateTransitionTests.cs
using Tasks.Domain.TaskAggregate;
using Tasks.Domain.TaskAggregate.Events;

// Alias pra resolver ambiguidade com System.Threading.Tasks.TaskStatus
using TaskStatus = Tasks.Domain.TaskAggregate.TaskStatus;

namespace Tasks.Domain.Tests.TaskAggregate;

/// <summary>
/// Testes das transições de estado (Start, Conclude, Reopen)
/// e dos eventos de domínio disparados.
/// </summary>
public class TaskItemStateTransitionTests
{
    [Fact]
    public void Start_DePending_PassaParaInProgress()
    {
        var task = TestData.CriarTaskValida();

        var result = task.Start();

        Assert.True(result.IsSuccess);
        Assert.Equal(TaskStatus.InProgress, task.Status);
    }

    [Fact]
    public void Start_QuandoJaConcluida_RetornaFalha()
    {
        var task = TestData.CriarTaskValida();
        task.Conclude();

        var result = task.Start();

        Assert.True(result.IsFailure);
        Assert.Equal(TaskStatus.Concluded, task.Status); // não muda
    }

    [Fact]
    public void Start_QuandoJaInProgress_RetornaFalha()
    {
        var task = TestData.CriarTaskValida();
        task.Start();

        var result = task.Start();

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Conclude_DeInProgress_PassaParaConcluded()
    {
        var task = TestData.CriarTaskValida();
        task.Start();

        var result = task.Conclude();

        Assert.True(result.IsSuccess);
        Assert.Equal(TaskStatus.Concluded, task.Status);
        Assert.NotNull(task.ConcludedAt);
    }

    [Fact]
    public void Conclude_DePending_PassaParaConcluded()
    {
        // Permite concluir direto, sem passar por InProgress
        var task = TestData.CriarTaskValida();

        var result = task.Conclude();

        Assert.True(result.IsSuccess);
        Assert.Equal(TaskStatus.Concluded, task.Status);
    }

    [Fact]
    public void Conclude_QuandoJaConcluida_RetornaFalha()
    {
        var task = TestData.CriarTaskValida();
        task.Conclude();

        var result = task.Conclude();

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Conclude_DisparaTaskConcludedEvent()
    {
        var task = TestData.CriarTaskValida();

        task.Conclude();

        var concludedEvent = task.DomainEvents.OfType<TaskConcludedEvent>().FirstOrDefault();
        Assert.NotNull(concludedEvent);
        Assert.Equal(task.Id, concludedEvent!.TaskId);
        Assert.Equal(task.ConcludedAt, concludedEvent.ConcludedAt);
    }

    [Fact]
    public void Reopen_DeConcluded_PassaParaInProgress()
    {
        var task = TestData.CriarTaskValida();
        task.Conclude();

        var result = task.Reopen();

        Assert.True(result.IsSuccess);
        Assert.Equal(TaskStatus.InProgress, task.Status);
        Assert.Null(task.ConcludedAt);
    }

    [Fact]
    public void Reopen_QuandoNaoEstaConcluida_RetornaFalha()
    {
        var task = TestData.CriarTaskValida();

        var result = task.Reopen();

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Reopen_DisparaTaskReopenedEvent()
    {
        var task = TestData.CriarTaskValida();
        task.Conclude();

        task.Reopen();

        var reopenedEvent = task.DomainEvents.OfType<TaskReopenedEvent>().FirstOrDefault();
        Assert.NotNull(reopenedEvent);
    }

    [Fact]
    public void ClearDomainEvents_LimpaLista()
    {
        var task = TestData.CriarTaskValida();
        Assert.NotEmpty(task.DomainEvents);

        task.ClearDomainEvents();

        Assert.Empty(task.DomainEvents);
    }
}
