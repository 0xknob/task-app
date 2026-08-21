// Tasks.Application/Tasks/Commands/CreateTask/CreateTaskCommand.cs
using Tasks.Domain.TaskAggregate;

namespace Tasks.Application.Tasks.Commands.CreateTask;

/// <summary>
/// COMMAND — representa a INTENÇÃO de criar uma tarefa.
///
/// POR QUE UM RECORD?
/// - Imutável: input não muda depois de chegar
/// - Igualdade por valor: dois CreateTask com mesmos dados são iguais
/// - Sintaxe curta: primary constructor no C# 12
///
/// POR QUE NÃO PASSA O DOMAIN DIRETO?
/// - Camada de Application é um "contrato" entre API e Domain
/// - Mudou o Domain? A API não precisa saber
/// - Adicionou validação de input? Só aqui, não no Domain
/// </summary>
public sealed record CreateTaskCommand(
    string Title,
    string Description,
    Priority Priority,
    DateTime DueDate);

/// <summary>
/// RESULTADO — o que a API recebe de volta.
///
/// Retornamos o ID da tarefa criada e o próprio objeto criado (já
/// populado) pra API devolver como response. Em outras empresas,
/// retornariam só o ID e a API faria um GET. Decisão de design.
/// </summary>
public sealed record CreateTaskResult(TaskItemId TaskId);
