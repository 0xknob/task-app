// Tasks.Domain/TaskAggregate/TaskItem.cs
using Tasks.Domain.common;
using Tasks.Domain.TaskAggregate.Assignee;
using Tasks.Domain.TaskAggregate.Comments;
using Tasks.Domain.TaskAggregate.Events;

namespace Tasks.Domain.TaskAggregate;

/// <summary>
/// RAIZ DO AGREGADO "TaskAggregate".
///
/// Esta classe é o PORTEIRO. Tudo que envolve tarefas passa por aqui.
/// Buscar uma TaskItem no repository? Retorna a raiz. Adicionar um
/// comentário? Passa pela TaskItem. Concluir? Idem.
///
/// O agregado garante:
/// 1. Todas as INVARIANTES (regras que SEMPRE valem)
/// 2. Coerência TRANSACIONAL (num save, tudo é commitado junto)
/// 3. Geração de EVENTOS de domínio
///
/// A raiz DETÉM as entidades internas (Comments) e value objects.
/// </summary>
public sealed class TaskItem : Entity<TaskItemId>
{
    // Coleção de comentários. ReadOnly no "set" externo — só a própria
    // TaskItem pode adicionar/remover. Por dentro usamos List pra gerenciar.
    private readonly List<Comment> _comments = new();

    public Title Title { get; private set; } = default!;
    public Description Description { get; private set; } = default!;
    public Priority Priority { get; private set; }
    public DueDate DueDate { get; private set; }
    public TaskStatus Status { get; private set; }
    public TaskAssignee? Assignee { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ConcludedAt { get; private set; }

    // Eventos pendentes. Cada vez que algo acontece, adicionamos aqui.
    // Quando a Application quiser persistir, ela consome e limpa a lista.
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Eventos acumulados. ReadOnly — o caller não pode modificar.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    /// <summary>
    /// Comentários expostos como IReadOnlyCollection. Por fora ninguém
    /// pode chamar .Add() direto na lista.
    /// </summary>
    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

    // Construtor privado — usado apenas pelos métodos fábrica.
    // Quem quiser criar uma TaskItem tem que passar pelo Create().
    private TaskItem() { }

    /// <summary>
    /// FACTORY METHOD principal — ponto de entrada pra criar tarefas.
    /// Recebe dados primitivos (string, Guid, DateTime) e usa os value
    /// objects pra validar. Se algum VO falhar, devolve Result.Fail.
    /// Se der certo, dispara TaskCreatedEvent.
    /// </summary>
    public static Result<TaskItem> Create(
        string titleText,
        string descriptionText,
        Priority priority,
        DateTime dueDateValue)
    {
        var titleResult = Title.Create(titleText);
        if (titleResult.IsFailure)
            return Result.Fail<TaskItem>(titleResult.Error!);

        var descriptionResult = Description.Create(descriptionText);
        if (descriptionResult.IsFailure)
            return Result.Fail<TaskItem>(descriptionResult.Error!);

        var dueDateResult = DueDate.Create(dueDateValue);
        if (dueDateResult.IsFailure)
            return Result.Fail<TaskItem>(dueDateResult.Error!);

        var task = new TaskItem
        {
            Id = TaskItemId.New(),
            Title = titleResult.Value!,
            Description = descriptionResult.Value!,
            Priority = priority,
            DueDate = dueDateResult.Value!,
            Status = TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        task.AddDomainEvent(new TaskCreatedEvent(
            task.Id, task.Title.Value, task.Priority, DateTime.UtcNow));

        return Result.Ok(task);
    }

    /// <summary>
    /// MÉTODO DE COMPORTAMENTO: concluir a tarefa.
    /// INVARIANTE: tarefa já concluída NÃO pode ser concluída de novo.
    /// </summary>
    public UnitResult Conclude()
    {
        if (Status == TaskStatus.Concluded)
            return Result.Fail("Tarefa já está concluída.");

        Status = TaskStatus.Concluded;
        ConcludedAt = DateTime.UtcNow;

        AddDomainEvent(new TaskConcludedEvent(
            Id, ConcludedAt.Value, DateTime.UtcNow));

        return Result.Ok();
    }

    /// <summary>
    /// Marca a tarefa como em progresso.
    /// </summary>
    public UnitResult Start()
    {
        if (Status == TaskStatus.Concluded)
            return Result.Fail("Tarefa concluída não pode ser iniciada novamente.");

        if (Status == TaskStatus.InProgress)
            return Result.Fail("Tarefa já está em progresso.");

        Status = TaskStatus.InProgress;
        return Result.Ok();
    }

    /// <summary>
    /// Reabrir uma tarefa concluída (volta pra InProgress).
    /// </summary>
    public UnitResult Reopen()
    {
        if (Status != TaskStatus.Concluded)
            return Result.Fail("Apenas tarefas concluídas podem ser reabertas.");

        Status = TaskStatus.InProgress;
        ConcludedAt = null;

        AddDomainEvent(new TaskReopenedEvent(Id, DateTime.UtcNow, DateTime.UtcNow));
        return Result.Ok();
    }

    /// <summary>
    /// Atribui a tarefa a um usuário.
    /// </summary>
    public UnitResult AssignTo(Guid userId)
    {
        if (userId == Guid.Empty)
            return Result.Fail("Usuário inválido.");

        Assignee = TaskAssignee.From(userId, DateTime.UtcNow);

        AddDomainEvent(new TaskAssignedEvent(
            Id, userId, Assignee.AssignedAt, DateTime.UtcNow));

        return Result.Ok();
    }

    /// <summary>
    /// Remove atribuição.
    /// </summary>
    public UnitResult Unassign()
    {
        if (Assignee is null)
            return Result.Fail("Tarefa não está atribuída.");

        Assignee = null;
        return Result.Ok();
    }

    /// <summary>
    /// Editar título (value object novo).
    /// </summary>
    public UnitResult ChangeTitle(string newTitleText)
    {
        if (Status == TaskStatus.Concluded)
            return Result.Fail("Tarefa concluída não pode ser editada.");

        var t = Title.Create(newTitleText);
        if (t.IsFailure)
            return Result.Fail(t.Error!);

        Title = t.Value!;
        return Result.Ok();
    }

    /// <summary>
    /// Adicionar comentário. Comment é ENTIDADE INTERNA, só nasce aqui.
    /// </summary>
    public UnitResult AddComment(Guid authorUserId, string content)
    {
        if (authorUserId == Guid.Empty)
            return Result.Fail("Autor do comentário é obrigatório.");

        var commentResult = Comment.Create(authorUserId, content);
        if (commentResult.IsFailure)
            return Result.Fail(commentResult.Error!);

        var comment = commentResult.Value!;
        _comments.Add(comment);

        AddDomainEvent(new CommentAddedEvent(
            Id, comment.Id, authorUserId, DateTime.UtcNow));

        return Result.Ok();
    }

    /// <summary>
    /// Limpa os eventos após a Application despachar.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    private void AddDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);
}
