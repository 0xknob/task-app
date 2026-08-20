// Tasks.Domain.Tests/TestData.cs
using Tasks.Domain.TaskAggregate;

namespace Tasks.Domain.Tests;

/// <summary>
/// Helper estático pra criar tarefas válidas nos testes.
/// Evita repetir os mesmos parâmetros válidos em todo teste.
/// Se a regra de criação mudar, muda num lugar só.
/// </summary>
internal static class TestData
{
    public static TaskItem CriarTaskValida()
    {
        var result = TaskItem.Create(
            titleText: "Tarefa de teste",
            descriptionText: "Descrição de teste",
            priority: Priority.Medium,
            dueDateValue: DateTime.UtcNow.AddDays(7));

        if (result.IsFailure)
            throw new InvalidOperationException($"Falha ao criar task válida: {result.Error}");

        return result.Value!;
    }

    public static TaskItem CriarTaskValida(string titulo)
    {
        var result = TaskItem.Create(
            titleText: titulo,
            descriptionText: "Descrição",
            priority: Priority.Medium,
            dueDateValue: DateTime.UtcNow.AddDays(7));

        return result.IsSuccess
            ? result.Value!
            : throw new InvalidOperationException(result.Error);
    }

    public static DateTime PrazoFuturo() => DateTime.UtcNow.AddDays(7);
}
