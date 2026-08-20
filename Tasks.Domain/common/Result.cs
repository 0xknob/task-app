// Tasks.Domain/common/Result.cs
namespace Tasks.Domain.common;

/// <summary>
/// Resultado de uma operação que pode dar certo OU errado SEM exception.
///
/// Por que Result<T> em vez de throw?
/// - Em DDD, validação é um fluxo ESPERADO, não uma exceção.
/// - Throw quebra o controle, é caro, e suja os logs.
/// - Result<T> obriga o caller a tratar o erro.
/// - É explícito: o tipo de retorno já diz "isto pode falhar".
///
/// É um padrão funcional adaptado pra C#. O nome vem de F# / Haskell.
/// </summary>
public sealed record Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    /// <summary>
    /// Inverso de IsSuccess. Existe pra deixar o código de checagem
    /// mais legível: "if (result.IsFailure) return ...".
    /// </summary>
    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);

    // Helper pra encadear resultados sem pyramid of doom.
    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> next)
        => IsSuccess ? next(Value!) : Result<TNew>.Failure(Error!);
}

/// <summary>
/// Result SEM valor de retorno. Sucesso ou falha.
/// Convenção de nome da programação funcional (F#, Haskell).
/// Existe separado de Result<T> pra evitar ambiguidade de nomes.
/// </summary>
public sealed record UnitResult
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    public bool IsFailure => !IsSuccess;

    private UnitResult(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static UnitResult Success() => new(true, null);
    public static UnitResult Failure(string error) => new(false, error);
}

/// <summary>
/// Factory helpers com nomes que NÃO colidem com Result<T>.
/// Usar explicitamente: Result.Ok&lt;T&gt;(value) ou Result.Fail&lt;T&gt;(msg).
/// </summary>
public static class Result
{
    /// <summary>Constrói um Result&lt;T&gt; de sucesso.</summary>
    public static Result<T> Ok<T>(T value) => Result<T>.Success(value);

    /// <summary>Constrói um Result&lt;T&gt; de falha.</summary>
    public static Result<T> Fail<T>(string error) => Result<T>.Failure(error);

    /// <summary>Constrói um UnitResult de sucesso.</summary>
    public static UnitResult Ok() => UnitResult.Success();

    /// <summary>Constrói um UnitResult de falha.</summary>
    public static UnitResult Fail(string error) => UnitResult.Failure(error);
}
