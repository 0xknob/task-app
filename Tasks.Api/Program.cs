// Tasks.Api/Program.cs
using Tasks.Application;
using Tasks.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Adiciona camada de Application (handlers CQRS)
builder.Services.AddApplication();

// Adiciona camada de Infrastructure (InMemory por padrão)
builder.Services.AddInfrastructure();

// Adiciona controllers + OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();