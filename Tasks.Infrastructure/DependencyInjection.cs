using Microsoft.Extensions.DependencyInjection;
using Tasks.Application.Abstractions;
using Tasks.Domain.TaskAggregate;
using Tasks.Infrastructure.Events;
using Tasks.Infrastructure.Persistence.InMemory;

namespace Tasks.Infrastructure;

/// <summary>
/// Helper de DI pra Infrastructure.
///
/// POR QUE EXISTE?
/// - A API vai chamar `services.AddInfrastructure()`.
/// - Aqui você escolhe: usa InMemory (dev/test) ou Cosmos (prod).
///
/// COMO TROCAR?
/// - Em dev/test: chama AddInMemoryPersistence().
/// - Em prod: vai ter AddCosmosPersistence(connectionString).
/// - Os Handlers NÃO MUDAM — eles dependem de IRepository, não da impl.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Por padrão, vamos usar InMemory pra rodar agora.
        // Quando você configurar Cosmos, vai comentar essa linha
        // e descomentar AddCosmosPersistence().
        return AddInMemoryPersistence(services);
    }

    /// <summary>
    /// Registra a persistência em memória (dev, testes).
    /// </summary>
    public static IServiceCollection AddInMemoryPersistence(this IServiceCollection services)
    {
        // Singleton: o Dictionary vive enquanto a aplicação roda.
        // Importante: Singleton pro repository porque o estado é compartilhado.
        services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();
        services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();

        // Dispatcher de eventos (loga no console).
        services.AddScoped<IDomainEventDispatcher, LoggingDomainEventDispatcher>();

        return services;
    }

    /*
    /// <summary>
    /// Registra persistência com Cosmos DB (produção).
    /// Descomente quando tiver a connection string do Azure.
    /// </summary>
    public static IServiceCollection AddCosmosPersistence(
        this IServiceCollection services,
        string connectionString,
        string databaseName,
        string containerName)
    {
        services.AddSingleton<ITaskRepository>(sp =>
            new CosmosTaskRepository(
                connectionString,
                databaseName,
                containerName));

        services.AddSingleton<IUnitOfWork>(sp =>
            new CosmosUnitOfWork(
                connectionString,
                databaseName,
                containerName));

        services.AddScoped<IDomainEventDispatcher, LoggingDomainEventDispatcher>();

        return services;
    }
    */
}
