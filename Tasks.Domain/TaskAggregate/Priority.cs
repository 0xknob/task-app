namespace Tasks.Domain.TaskAggregate;

/// <summary>
/// Prioridade da tarefa. Value Object baseado em enum.
/// 
/// Usamos ENUM no domínio quando:
/// - O conjunto de valores é FINITO e ESTÁVEL
/// - Os valores não mudam em runtime
/// - Não têm comportamento próprio (só identificadores)
/// </summary>
public enum Priority
{
    Low = 0,
    Medium = 1,
    High = 2
}
