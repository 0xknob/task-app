namespace Tasks.Domain.TaskAggregate;

/// <summary>
/// Status de uma tarefa. É um value object porque não tem identidade.
/// 
/// Máquina de estado:
/// 
///   [Pending] --Iniciar--> [InProgress] --Concluir--> [Concluded]
///       \                                                /
///        \------------------Reabrir---------------------/
/// 
/// Regras:
/// - Tarefa Concluída pode ser Reabrir (volta pra InProgress)
/// - Tarefa Concluída NÃO pode ser Iniciada (já esteve em progresso)
/// - Tarefa Pending pode ser Iniciada ou Concluída direto
/// </summary>
public enum TaskStatus
{
    Pending = 0,
    InProgress = 1,
    Concluded = 2
}
