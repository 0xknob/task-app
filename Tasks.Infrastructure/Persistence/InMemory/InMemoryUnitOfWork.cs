using Tasks.Application.Abstractions;
using Tasks.Domain.common;

namespace Tasks.Infrastructure.Persistence.InMemory;

/// <summary>
/// UNIT OF WORK IN-MEMORY.
///
/// PRA QUE SERVE EM MEMÓRIA?
/// - Em Cosmos: agrupa várias operações num batch transacional.
/// - Em SQL: BEGIN COMMIT.
/// - Em memória: NÃO PRECISA. Tudo já está "comitado" no Dictionary.
///
/// POR QUE EXISTIR?
/// - Pra manter a interface. Os Handlers não sabem se é Cosmos ou memória.
/// - Pra que DI funcione: a API registra `IUnitOfWork → InMemoryUnitOfWork`
///   em dev, `IUnitOfWork → CosmosUnitOfWork` em prod. Mesma interface.
///
/// DETALHE IMPORTANTE:
/// - Em Cosmos, SaveChangesAsync EFETIVAMENTE envia o batch.
/// - Aqui, retorna Ok sem fazer nada (no-op). Mas o Handler chama,
///   o código roda, o sistema funciona.
/// </summary>
public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public Task<UnitResult> SaveChangesAsync(CancellationToken ct = default)
    {
        // No-op: tudo já está persistido no Dictionary.
        // Só retorna sucesso pra manter o contrato.
        return Task.FromResult(Result.Ok());
    }
}
