using Tasks.Domain.common;

namespace Tasks.Domain.TaskAggregate;

/// <summary>
/// Título de uma tarefa. Value Object IMUTÁVEL.
/// 
/// Regras (invariantes):
/// - Não pode ser vazio
/// - Não pode ser só espaços em branco
/// - Máximo 200 caracteres
/// 
/// Onde a validação acontece? NO CONSTRUTOR. 
/// Se você conseguiu instanciar um Title, ele é válido.
/// É impossível ter um Title inválido em memória.
/// </summary>
public sealed record Title
{
    public const int MaxLength = 200;

    public string Value { get; }

    // Construtor PRIVADO — Title SÓ pode ser criado via Create().
    // Isso garante que ninguém crie Title inválido por descuido.
    private Title(string value) => Value = value;

    /// <summary>
    /// Factory method. Tenta criar um Title, retorna erro se inválido.
    /// 
    /// Por que não lançar exception?
    /// - Em DDD, erros de validação são EXPECTADOS, não excepcionais.
    /// - É mais limpo o caller receber um Result e decidir o que fazer.
    /// - Vamos usar Result<T> mais pra frente na Application.
    /// </summary>
    public static Result<Title> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Fail<Title>("Título não pode ser vazio.");

        if (value.Length > MaxLength)
            return Result.Fail<Title>($"Título não pode ter mais de {MaxLength} caracteres.");

        return Result.Ok(new Title(value.Trim()));
    }
}
