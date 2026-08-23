# 🚀 task-app — Microsserviço de Gestão de Tarefas (.NET 10)

> **Template de microsserviço com DDD tático, CQRS, testes abrangentes e persistência plugável.**
> Construído como projeto de aprendizado, evoluído pra template reutilizável.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/tests-69_passing-success)]()
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Architecture](https://img.shields.io/badge/architecture-DDD_Lite-blueviolet)]()

---

## 🎯 O que é

Um microsserviço de **gestão de tarefas** implementado em .NET 10 com:

- **DDD tático** (Domain-Driven Design)
- **CQRS** (Command Query Responsibility Segregation)
- **Repository Pattern + Unit of Work**
- **Domain Events** com dispatch desacoplado
- **69 testes** cobrindo todas as invariantes do domínio
- **Persistência In-Memory** (pronto pra trocar por Cosmos DB / SQL)

O produto é simples, mas o **esqueleto é o foco**. Você pode reusar essa estrutura pra qualquer domínio: e-commerce, helpdesk, marketplace, OKRs, agendamentos...

---

## 🏗️ Arquitetura

```
┌─────────────────────────────────────────────────────┐
│  Tasks.Api         (HTTP, Controllers, DTOs)        │  ← Tradução
└─────────────────────────────────────────────────────┘
                       │
┌─────────────────────────────────────────────────────┐
│  Tasks.Application  (CQRS Handlers, Abstractions)   │  ← Orquestração
└─────────────────────────────────────────────────────┘
                       │
┌─────────────────────────────────────────────────────┐
│  Tasks.Domain       (Aggregates, VOs, Events)       │  ← Regras de negócio
└─────────────────────────────────────────────────────┘
                       ▲
┌─────────────────────────────────────────────────────┐
│  Tasks.Infrastructure (InMemory / SQL / Cosmos)     │  ← Adaptadores
└─────────────────────────────────────────────────────┘
                       │
        ┌──────────────────────────────┐
        │  Tasks.Domain.Tests (xUnit)  │  ← 69 testes
        └──────────────────────────────┘
```

**Regra de dependência:** setas apontam para baixo. Domain não conhece ninguém acima. Application depende só de Domain. Infrastructure implementa interfaces. Api é fina.

---

## 📂 Estrutura de pastas

```
task-app/
├── Tasks.Domain/                    ← Coração do sistema
│   ├── common/                      ← Entity, IDomainEvent, Result
│   └── TaskAggregate/               ← Aggregate "Task"
│       ├── TaskItem.cs              ← Raiz do agregado (9 comportamentos)
│       ├── TaskItemId.cs            ← ID tipado
│       ├── Title.cs, Description.cs, Priority.cs, DueDate.cs, TaskStatus.cs
│       ├── ITaskRepository.cs       ← Contrato de persistência
│       ├── Assignee/TaskAssignee.cs
│       ├── Comments/Comment.cs      ← Entidade interna
│       └── Events/                  ← 5 domain events
│
├── Tasks.Application/               ← CQRS
│   ├── Abstractions/                ← IUnitOfWork, IDomainEventDispatcher
│   ├── Tasks/Commands/              ← CreateTask, ConcludeTask, AssignTask, AddComment
│   ├── Tasks/Queries/               ← GetTaskById, ListTasks
│   └── DependencyInjection.cs
│
├── Tasks.Infrastructure/            ← Adaptadores
│   ├── Persistence/InMemory/        ← Repositório em memória
│   ├── Events/LoggingDomainEventDispatcher.cs
│   └── DependencyInjection.cs
│
├── Tasks.Api/                       ← HTTP
│   ├── Controllers/TasksController.cs
│   ├── Requests/, Responses/        ← DTOs
│   ├── Mapping/TaskMapping.cs
│   └── Program.cs
│
└── Tasks.Domain.Tests/              ← 69 testes xUnit
```

---

## 🚀 Como rodar

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build

```bash
dotnet build TasksApp.slnx
```

### Testes

```bash
dotnet test
# → 69 testes passando em ~2 segundos
```

### API

```bash
cd Tasks.Api
dotnet run
```

A API sobe em `https://localhost:5001` (ou porta configurada em `launchSettings.json`).

---

## 📡 Endpoints

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/tasks` | Criar tarefa |
| `GET` | `/api/tasks/{id}` | Buscar tarefa por ID |
| `GET` | `/api/tasks?status=&priority=&assigneeUserId=` | Listar tarefas com filtros |
| `POST` | `/api/tasks/{id}/conclude` | Concluir tarefa |
| `POST` | `/api/tasks/{id}/assign` | Atribuir tarefa a um usuário |
| `POST` | `/api/tasks/{id}/comments` | Adicionar comentário |

### Exemplo: criar tarefa

```bash
curl -X POST http://localhost:5000/api/tasks \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Estudar DDD",
    "description": "Ler o livro azul do Evans",
    "priority": "High",
    "dueDate": "2026-09-01T00:00:00Z"
  }'
```

Resposta (201 Created):
```json
{ "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
```

---

## 🧠 Conceitos aplicados

| Conceito | Onde mora | Por quê |
|---|---|---|
| **Aggregate Root** | `TaskItem.cs` | Porteiro do agregado. Toda mudança passa por ele. |
| **Value Objects** | `Title`, `Description`, `DueDate`, etc. | Imutáveis, validados no construtor. |
| **Invariantes** | Métodos do `TaskItem` | "Não pode concluir tarefa concluída" mora num lugar só. |
| **Domain Events** | `Events/*.cs` | Fatos do passado, nome o passado. Disparados pelo agregado. |
| **Repository Pattern** | `ITaskRepository.cs` | Domain não conhece o banco. Implementação fica na Infrastructure. |
| **Unit of Work** | `IUnitOfWork.cs` | Commit transacional desacoplado do repository. |
| **CQRS** | `Commands/` e `Queries/` | Commands mudam estado, Queries só leem. |
| **Result Pattern** | `Result<T>` / `UnitResult` | Substitui exception por valor de retorno explícito. |
| **Factory Method** | `TaskItem.Create()` | Construtor privado + factory que valida. |

---

## 🧪 Testes

69 testes cobrindo **todas as invariantes** do Domain:

- 9 testes de `Title` (validação, trim, igualdade)
- 5 testes de `Description`
- 5 testes de `DueDate` (incluindo `IsOverdue`)
- 5 testes de `TaskItemId` (igualdade por valor)
- 9 testes de `Comment`
- 10 testes de `TaskItem.Create()` (estado inicial, eventos)
- 13 testes de transições de estado (Start, Conclude, Reopen)
- 6 testes de atribuição
- 9 testes de edição e comentários

**Tempo de execução:** ~2 segundos. Sem dependências externas.

---

## 🔄 Trocar de persistência

Hoje usa **InMemory**. Pra usar Cosmos DB:

1. Adicionar pacote:
   ```xml
   <PackageReference Include="Microsoft.Azure.Cosmos" Version="3.*" />
   ```

2. Implementar `CosmosTaskRepository : ITaskRepository`
3. Implementar `CosmosUnitOfWork : IUnitOfWork`
4. Trocar no `DI`:
   ```csharp
   services.AddCosmosPersistence(connectionString, databaseName, containerName);
   ```

**Os Handlers NÃO MUDAM.** Inversão de Dependência em ação.

---

## 📐 Decisões arquiteturais (ADRs)

Decisões importantes ficam registradas em [`docs/adr/`](docs/adr/) no formato
MADR (Markdown ADR). Cada ADR documenta **contexto, decisão, consequências e
alternativas rejeitadas** — não só "o que fizemos", mas "por que fizemos".

| # | Título | Status |
|---|---|---|
| [0001](docs/adr/0001-accept-enums-as-strings.md) | Aceitar enums como strings no JSON | ✅ Aceito |

---

## 📚 Roadmap

- [x] Domain completo com testes
- [x] Application com CQRS
- [x] Infrastructure In-Memory
- [x] API HTTP
- [ ] Front-end React (Material Design M2 + Storybook)
- [ ] Persistência Cosmos DB
- [ ] Deploy Azure (App Service + Cosmos + Pipeline)

---

## 🤝 Contribuindo

Esse é um projeto de aprendizado. Sugestões de melhoria são bem-vindas.

Veja [CONTRIBUTING.md](CONTRIBUTING.md) (em construção).

---

## 📄 Licença

MIT — veja [LICENSE](LICENSE) pra detalhes.

---

## 👤 Autor

**0xknob** — dev em formação, focado em microsserviços .NET + React + Azure.

Construído como projeto de aprendizado com mentoria de IA. Cada commit conta a história da evolução.

---

> *"O esqueleto é o produto. O domínio é só o exemplo que prova que ele funciona."*