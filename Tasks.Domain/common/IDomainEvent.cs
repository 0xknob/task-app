// Tasks.Domain/common/IDomainEvent.cs
namespace Tasks.Domain.common;

/// <summary>
/// Marca qualquer classe como sendo um EVENTO DE DOMÍNIO.
/// 
/// O que é um domain event?
/// - É um FATO que aconteceu no passado. Por isso o nome do método 
///   geralmente termina em "ed" (Created, Concluded, Assigned).
/// - Carrega informações suficientes pra que OUTRAS partes do sistema
///   possam reagir sem precisar consultar o banco.
/// - É gerado DENTRO do agregado, normalmente quando uma invariante
///   muda ou uma ação importante acontece.
/// 
/// Quem dispara? O próprio agregado.
/// Quem consome? Camadas externas (Application, Infrastructure, 
/// integração, UI, etc).
/// 
/// Importante: o Domain não decide QUEM ou COMO vai consumir.
/// Ele só diz "isso aconteceu".
/// </summary>
public interface IDomainEvent
{
    // Momento em que o evento foi gerado (UTC).
    DateTime OccurredOn { get; }
}