// Tasks.Application/Abstractions/IUnitOfWork.cs
using Tasks.Domain.common;

namespace Tasks.Application.Abstractions;

/// <summary>
/// UNIT OF WORK — agrupa alterações num único commit.
///
/// POR QUE EXISTE?
/// Repository cuida de UMA entidade. Unit of Work cuida do COMMIT
/// que pode envolver várias operações (1 task + 3 eventos, por ex).
///
/// PADRÃO:
/// 1. Repository.GetById(taskId)
/// 2. task.MétodoQueMudaEstado()
/// 3. Repository.UpdateAsync(task)
/// 4. UnitOfWork.SaveChangesAsync()  ← commit transacional
///
/// Em Cosmos DB / SQL Server, isso vira BEGIN COMMIT ou batch.
/// Em testes, vira no-op (ou só limpa eventos).
///
/// POR QUE É INTERFACE?
/// Implementação real fica na Infrastructure. Aqui só o contrato.
/// </summary>
public interface IUnitOfWork
{
    Task<UnitResult> SaveChangesAsync(CancellationToken ct = default);
}
