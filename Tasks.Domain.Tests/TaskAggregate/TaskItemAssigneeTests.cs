// Tasks.Domain.Tests/TaskAggregate/TaskItemAssigneeTests.cs
using Tasks.Domain.TaskAggregate;
using Tasks.Domain.TaskAggregate.Assignee;
using Tasks.Domain.TaskAggregate.Events;

namespace Tasks.Domain.Tests.TaskAggregate;

/// <summary>
/// Testes de atribuição de tarefas (AssignTo, Unassign).
/// </summary>
public class TaskItemAssigneeTests
{
    [Fact]
    public void AssignTo_ComUserIdValido_Atribui()
    {
        var task = TestData.CriarTaskValida();
        var userId = Guid.NewGuid();

        var result = task.AssignTo(userId);

        Assert.True(result.IsSuccess);
        Assert.NotNull(task.Assignee);
        Assert.Equal(userId, task.Assignee!.UserId);
    }

    [Fact]
    public void AssignTo_ComGuidEmpty_RetornaFalha()
    {
        var task = TestData.CriarTaskValida();

        var result = task.AssignTo(Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Null(task.Assignee);
    }

    [Fact]
    public void AssignTo_DisparaTaskAssignedEvent()
    {
        var task = TestData.CriarTaskValida();
        var userId = Guid.NewGuid();

        task.AssignTo(userId);

        var assignedEvent = task.DomainEvents.OfType<TaskAssignedEvent>().FirstOrDefault();
        Assert.NotNull(assignedEvent);
        Assert.Equal(userId, assignedEvent!.AssigneeUserId);
    }

    [Fact]
    public void AssignTo_DuasVezes_SubstituiAssigneeAnterior()
    {
        var task = TestData.CriarTaskValida();
        var primeiroUser = Guid.NewGuid();
        var segundoUser = Guid.NewGuid();

        task.AssignTo(primeiroUser);
        task.AssignTo(segundoUser);

        Assert.Equal(segundoUser, task.Assignee!.UserId);
    }

    [Fact]
    public void Unassign_QuandoTemAssignee_Remove()
    {
        var task = TestData.CriarTaskValida();
        task.AssignTo(Guid.NewGuid());

        var result = task.Unassign();

        Assert.True(result.IsSuccess);
        Assert.Null(task.Assignee);
    }

    [Fact]
    public void Unassign_QuandoNaoTemAssignee_RetornaFalha()
    {
        var task = TestData.CriarTaskValida();

        var result = task.Unassign();

        Assert.True(result.IsFailure);
    }
}
