// Tasks.Application/DependencyInjection.cs
using Microsoft.Extensions.DependencyInjection;
using Tasks.Application.Tasks.Commands.AddComment;
using Tasks.Application.Tasks.Commands.AssignTask;
using Tasks.Application.Tasks.Commands.ConcludeTask;
using Tasks.Application.Tasks.Commands.CreateTask;
using Tasks.Application.Tasks.Queries.GetTaskById;
using Tasks.Application.Tasks.Queries.ListTasks;

namespace Tasks.Application;

/// <summary>
/// Helper pra registrar handlers no container de DI.
///
/// POR QUE EXISTE?
/// Evita poluir o Program.cs da API com N "AddScoped".
/// Quem chama: API na Startup. Como: services.AddApplication();
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Commands
        services.AddScoped<CreateTaskHandler>();
        services.AddScoped<ConcludeTaskHandler>();
        services.AddScoped<AssignTaskHandler>();
        services.AddScoped<AddCommentHandler>();

        // Queries
        services.AddScoped<GetTaskByIdHandler>();
        services.AddScoped<ListTasksHandler>();

        return services;
    }
}
