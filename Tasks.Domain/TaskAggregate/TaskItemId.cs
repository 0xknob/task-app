// Tasks.Domain/TaskAggregate/TaskItemId.cs
namespace Tasks.Domain.TaskAggregate;

/// <summary>
/// Identidade TIPADA de uma TaskItem.
///
/// Por que não usar Guid direto?
/// - Type safety: o compilador não deixa você passar o ID de uma
///   entidade em lugar de outra.
/// - Clareza: TaskItemId.From(...) é mais legível que new Guid(...).
/// - Encapsulamento: se um dia o ID virar Ulid ou outro formato,
///   o resto do código não muda.
///
/// Value Object: NÃO tem identidade. Dois TaskItemId com mesmo Guid
/// são o mesmo "valor" e intercambiáveis.
/// </summary>
public readonly record struct TaskItemId(Guid Value)
{
    public static TaskItemId New() => new(Guid.NewGuid());

    public static TaskItemId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
