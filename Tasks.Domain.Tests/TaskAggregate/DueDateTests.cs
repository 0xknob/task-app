// Tasks.Domain.Tests/TaskAggregate/DueDateTests.cs
using Tasks.Domain.TaskAggregate;

namespace Tasks.Domain.Tests.TaskAggregate;

public class DueDateTests
{
    [Fact]
    public void Create_ComDataFutura_RetornaSucesso()
    {
        var dataFutura = DateTime.UtcNow.AddDays(7);

        var result = DueDate.Create(dataFutura);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_ComDataNoPassado_RetornaFalha()
    {
        var dataPassada = DateTime.UtcNow.AddDays(-7);

        var result = DueDate.Create(dataPassada);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_SalvaEmUtc()
    {
        var dataLocal = new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Local);

        var result = DueDate.Create(dataLocal);

        Assert.True(result.IsSuccess);
        Assert.Equal(DateTimeKind.Utc, result.Value!.Value.Kind);
    }

    [Fact]
    public void IsOverdue_QuandoDataPassou_RetornaTrue()
    {
        var dueDate = DueDate.Create(DateTime.UtcNow.AddSeconds(2)).Value!;

        Thread.Sleep(2100);

        Assert.True(dueDate.IsOverdue());
    }

    [Fact]
    public void IsOverdue_QuandoDataAindaNaoChegou_RetornaFalse()
    {
        var dueDate = DueDate.Create(DateTime.UtcNow.AddDays(1)).Value!;

        Assert.False(dueDate.IsOverdue());
    }
}
