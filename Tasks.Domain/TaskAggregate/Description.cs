using Tasks.Domain.common;

namespace Tasks.Domain.TaskAggregate;

public sealed record Description
{
    public const int MaxLength = 2000;

    public string Value { get; }

    private Description(string value) => Value = value;

    public static Result<Description> Create(string value)
    {
        // Descrição PODE ser vazia (tarefa sem descrição é ok).
        // Por isso não checamos IsNullOrWhiteSpace aqui.
        if (value is null)
            value = string.Empty;

        if (value.Length > MaxLength)
            return Result.Fail<Description>($"Descrição não pode ter mais de {MaxLength} caracteres.");

        return Result.Ok(new Description(value.Trim()));
    }
}
