// Tasks.Api/Controllers/TasksController.cs
using Microsoft.AspNetCore.Mvc;
using Tasks.Api.Mapping;
using Tasks.Api.Requests;
using Tasks.Application.Tasks.Commands.AddComment;
using Tasks.Application.Tasks.Commands.AssignTask;
using Tasks.Application.Tasks.Commands.ConcludeTask;
using Tasks.Application.Tasks.Commands.CreateTask;
using Tasks.Application.Tasks.Queries.GetTaskById;
using Tasks.Application.Tasks.Queries.ListTasks;
using Tasks.Domain.common;

namespace Tasks.Api.Controllers;

/// <summary>
/// Controller de tarefas. Burro de propósito:
/// - Recebe request DTO
/// - Chama Handler
/// - Mapeia Result → HTTP
///
/// SEM regra de negócio aqui. SEM validação de domínio.
/// Apenas tradução HTTP ↔ Application.
/// </summary>
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTaskRequest request,
        [FromServices] CreateTaskHandler handler,
        CancellationToken ct)
    {
        // Validação automática do ASP.NET — se request for inválido,
        // retorna 400 antes de chegar aqui.
        var command = new CreateTaskCommand(
            request.Title,
            request.Description,
            request.Priority,
            request.DueDate);

        var result = await handler.HandleAsync(command, ct);

        if (result.IsFailure)
            return ResultToHttp(result.Error!);

        // 201 Created com a localização do novo recurso
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.TaskId.Value },
            new { id = result.Value!.TaskId.Value });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] GetTaskByIdHandler handler,
        CancellationToken ct)
    {
        var query = new GetTaskByIdQuery(new Domain.TaskAggregate.TaskItemId(id));
        var result = await handler.HandleAsync(query, ct);

        if (result.IsFailure)
            return ResultToHttp(result.Error!);

        return Ok(result.Value!.ToResponse());
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Domain.TaskAggregate.TaskStatus? status,
        [FromQuery] Domain.TaskAggregate.Priority? priority,
        [FromQuery] Guid? assigneeUserId,
        [FromServices] ListTasksHandler handler,
        CancellationToken ct)
    {
        var query = new ListTasksQuery(status, priority, assigneeUserId);
        var result = await handler.HandleAsync(query, ct);

        if (result.IsFailure)
            return ResultToHttp(result.Error!);

        var response = result.Value!.Select(t => t.ToResponse()).ToList();
        return Ok(response);
    }

    [HttpPost("{id:guid}/conclude")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Conclude(
        Guid id,
        [FromServices] ConcludeTaskHandler handler,
        CancellationToken ct)
    {
        var command = new ConcludeTaskCommand(new Domain.TaskAggregate.TaskItemId(id));
        var result = await handler.HandleAsync(command, ct);

        return result.IsSuccess ? NoContent() : ResultToHttp(result.Error!);
    }

    [HttpPost("{id:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Assign(
        Guid id,
        [FromBody] AssignTaskRequest request,
        [FromServices] AssignTaskHandler handler,
        CancellationToken ct)
    {
        var command = new AssignTaskCommand(
            new Domain.TaskAggregate.TaskItemId(id),
            request.AssigneeUserId);

        var result = await handler.HandleAsync(command, ct);
        return result.IsSuccess ? NoContent() : ResultToHttp(result.Error!);
    }

    [HttpPost("{id:guid}/comments")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddComment(
        Guid id,
        [FromBody] AddCommentRequest request,
        [FromServices] AddCommentHandler handler,
        CancellationToken ct)
    {
        var command = new AddCommentCommand(
            new Domain.TaskAggregate.TaskItemId(id),
            request.AuthorUserId,
            request.Content);

        var result = await handler.HandleAsync(command, ct);
        return result.IsSuccess ? NoContent() : ResultToHttp(result.Error!);
    }

    /// <summary>
    /// Converte mensagem de erro do Domain em HTTP status apropriado.
    /// 404 para "não encontrado", 400 para validação.
    /// </summary>
    private IActionResult ResultToHttp(string error)
    {
        if (error.Contains("não encontrada", StringComparison.OrdinalIgnoreCase))
            return NotFound(new { error });

        return BadRequest(new { error });
    }
}