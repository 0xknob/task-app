// Tasks.Domain/common/Entity.cs
namespace Tasks.Domain.common;

/// <summary>
/// Classe base para todas as entidades do domínio.
/// 
/// O que define uma entidade em DDD?
/// - Tem IDENTIDADE (um ID) — duas entidades com mesmos dados mas IDs diferentes
///   NÃO são a mesma coisa.
/// - Tem CICLO DE VIDA — nasce, muda de estado, pode ser "removida".
/// - Tem COMPORTAMENTOS — não é só um saco de dados (DTO).
/// 
/// Por que uma classe base?
/// Pra evitar repetir o conceito de "tenho um ID" em todas as entidades.
/// É um truque de orientação a objetos: generalizar o que é comum.
/// </summary>
public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;

    // Comparação de entidades é por ID, não por valor.
    // Dois clientes com mesmo nome são clientes DIFERENTES (IDs diferentes).
    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public static bool operator ==(Entity<TId>? a, Entity<TId>? b)
        => a is null ? b is null : a.Equals(b);

    public static bool operator !=(Entity<TId>? a, Entity<TId>? b) => !(a == b);

    public override int GetHashCode() => Id?.GetHashCode() ?? 0;
}