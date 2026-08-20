// Tasks.Domain.Tests/TaskAggregate/TaskItemIdTests.cs
using Tasks.Domain.TaskAggregate;

namespace Tasks.Domain.Tests.TaskAggregate;

public class TaskItemIdTests
{
    [Fact]
    public void New_GeraGuidNaoVazio()
    {
        var id = TaskItemId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    [Fact]
    public void New_GeraGuidsDiferentes()
    {
        var a = TaskItemId.New();
        var b = TaskItemId.New();

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void From_ReconstroiIdAPartirDeGuid()
    {
        var guid = Guid.NewGuid();

        var id = TaskItemId.From(guid);

        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void TaskItemId_IgualPorValor()
    {
        var guid = Guid.NewGuid();
        var a = TaskItemId.From(guid);
        var b = TaskItemId.From(guid);

        Assert.Equal(a, b);
    }

    [Fact]
    public void ToString_RetornaGuidEmFormatoString()
    {
        var guid = Guid.NewGuid();
        var id = TaskItemId.From(guid);

        Assert.Equal(guid.ToString(), id.ToString());
    }
}
