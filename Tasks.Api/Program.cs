// Tasks.Api/Program.cs
using System.Text.Json.Serialization;
using Tasks.Application;
using Tasks.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Adiciona camada de Application (handlers CQRS)
builder.Services.AddApplication();

// Adiciona camada de Infrastructure (InMemory por padrão)
builder.Services.AddInfrastructure();

// Adiciona controllers + OpenAPI
builder.Services
    .AddControllers()
    // Aceita enums como string no JSON de ENTRADA (request body).
    // A SAÍDA continua serializando como string (via .ToString() no mapping).
    // Sem isso, o front teria que enviar "priority": 2 em vez de "priority": "High".
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// CORS: libera o front-end em http://localhost:5173 (Vite/React)
// a chamar esta API. Em produção, o origin será o do Azure App Service.
// Sem isso, o navegador BLOQUEIA a request por segurança.
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevFront", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:6006")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// IMPORTANTE: UseCors vem ANTES de UseHttpsRedirection/UseAuthorization/MapControllers
app.UseCors("DevFront");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();