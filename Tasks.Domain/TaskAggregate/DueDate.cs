using Tasks.Domain.common;

namespace Tasks.Domain.TaskAggregate;

/// <summary>
/// Prazo de uma tarefa. Value Object que carrega regras de calendário.
/// 
/// Pra que serve um VO de data se DateTime já existe?
/// - Permite regras de domínio morarem no tipo (ex: "não pode ser no passado")
/// - Modela a INTENÇÃO (prazo), não o tipo primitivo (DateTime)
/// - Type-safety: DueDate não é confundível com CreatedAt
/// </summary>
public readonly record struct DueDate
{
    public DateTime Value { get; }

    private DueDate(DateTime value) => Value = value;

    public static Result<DueDate> Create(DateTime value)
    {
        // Regra de domínio: prazo não pode ser no passado distante.
        // Usamos UTC pra evitar problemas de timezone.
        var now = DateTime.UtcNow;
        if (value < now.AddMinutes(-1)) // tolerância de 1 min pra clocks
            return Result.Fail<DueDate>("Prazo não pode ser no passado.");

        return Result.Ok(new DueDate(value.ToUniversalTime()));
    }

    public bool IsOverdue() => Value < DateTime.UtcNow && Value != default;

    public static DueDate Unset => new(DateTime.MaxValue);
}
