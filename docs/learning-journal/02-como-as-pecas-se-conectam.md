# Como as Peças se Conectam — Mapa Mental do Sistema

> Documento visual / conceitual. Sem código. Pra entender o **fluxo** de uma
> request do começo ao fim, e como cada framework conversa com o outro.
>
> **Pra quem é esse texto?** Pra você (autor) revisar antes da call com o
> Andre. Pra qualquer dev que vai abrir o repo e quer entender a arquitetura
> em 10 minutos sem rodar o código.

---

## Índice

1. [Visão geral — o sistema em uma imagem](#visao-geral)
2. [Stack por camada (quem é quem)](#stack-por-camada)
3. [Request lifecycle — uma chamada de ponta a ponta](#request-lifecycle)
4. [Mutations — quando algo muda](#mutations)
5. [Caminhos paralelos — o que NÃO se fala](#caminhos-paralelos)
6. [🎯 Roteiro pra call com o Andre (15min)](#roteiro-andre)

---

<a id="visao-geral"></a>
## Visão geral — o sistema em uma imagem

```
┌──────────────────────────────────────────────────────────────────┐
│                           NAVEGADOR                              │
│  React 19 + TypeScript + Material UI (M2) + TanStack Query        │
│                                                                  │
│  http://localhost:5173                                           │
└───────────────────────────┬──────────────────────────────────────┘
                            │
                            │ HTTP/JSON (axios)
                            │ CORS preflight (OPTIONS)
                            ▼
┌──────────────────────────────────────────────────────────────────┐
│                     ASP.NET CORE 10 (Kestrel)                    │
│  http://localhost:5000                                           │
│                                                                  │
│  ┌────────────────────────────────────────────────────────┐     │
│  │ Pipeline de middlewares                                  │     │
│  │ UseCors → UseHttpsRedir → UseAuthorization → MapController  │
│  └────────────────────────┬───────────────────────────────┘     │
│                           ▼                                      │
│  ┌────────────────────────────────────────────────────────┐     │
│  │ Tasks.Api — Controllers                                 │     │
│  │ Recebe DTO, chama Handler, devolve IActionResult        │     │
│  └────────────────────────┬───────────────────────────────┘     │
│                           ▼                                      │
│  ┌────────────────────────────────────────────────────────┐     │
│  │ Tasks.Application — CQRS                                │     │
│  │ Commands → Handlers que mutam                          │     │
│  │ Queries → Handlers que leem                             │     │
│  └────────────────────────┬───────────────────────────────┘     │
│                           ▼                                      │
│  ┌────────────────────────────────────────────────────────┐     │
│  │ Tasks.Domain — Regras de negócio PURAS                  │     │
│  │ TaskItem aggregate + VOs + Domain Events                │     │
│  │ SEM dependências externas                               │     │
│  └────────────────────────┬───────────────────────────────┘     │
│                           ▲                                      │
│  ┌────────────────────────────────────────────────────────┐     │
│  │ Tasks.Infrastructure — Adaptadores                       │     │
│  │ ITaskRepository → InMemoryTaskRepository (HOJE)        │     │
│  │ IDomainEventDispatcher → LoggingDispatcher              │     │
│  │ Troca por Cosmos/SQL amanhã, ZERO muda acima           │     │
│  └────────────────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────────────────┘
```

**A pirâmide de dependência é a parte mais importante:**

```
        Api  ────────┐
                      ▼ depende de
        Infrastructure
                      ▼ depende de
        Application
                      ▼ depende de
        Domain  ← puro, NUNCA depende de ninguém
```

> **Quem está abaixo não sabe que quem está acima existe.** O Domain não
> importa nada de Infrastructure. Por isso dá pra trocar o banco sem mexer
> nas regras de negócio.

---

<a id="stack-por-camada"></a>
## Stack por camada (quem é quem)

### Backend (.NET 10)

| Camada | Projeto .NET | Responsabilidade | Depende de |
|---|---|---|---|
| **Domain** | `Tasks.Domain` | Entidades, VOs, eventos, regras de negócio | Nada (puro) |
| **Application** | `Tasks.Application` | CQRS handlers, orquestração | Domain |
| **Infrastructure** | `Tasks.Infrastructure` | Repositórios, dispatcher, persistência | Application, Domain |
| **API** | `Tasks.Api` | HTTP, controllers, DTOs, mapeamento | Infrastructure, Application, Domain |

### Frontend (React 19)

| Camada | Pasta `src/` | Responsabilidade |
|---|---|---|
| **HTTP** | `api/` | Axios client + funções por endpoint |
| **State de servidor** | `TanStack Query` | Cache, refetch, invalidação (NÃO Redux) |
| **State local** | `useState` / `useReducer` | Formulários, UI |
| **Forms** | `pages/CreateTaskPage` + RHF + Zod | Validação tipada |
| **UI** | `components/` + `theme/` | Componentes visuais + tema M2 |
| **Roteamento** | `react-router-dom` | SPA (URL muda sem reload) |
| **Documentação** | `*.stories.tsx` | Storybook por componente |

---

<a id="request-lifecycle"></a>
## Request lifecycle — uma chamada de ponta a ponta

**Cenário:** usuário clica numa tarefa na lista → abre o detalhe.

```
PASSO 1 (navegador, ~1ms)
Usuário clica no <TaskCard onClick={navigate(`/tasks/${id}`)}>
↓
React Router muda a URL pra /tasks/abc-123
↓
<TaskDetailPage> renderiza

PASSO 2 (React, ~5ms)
useParams() captura { id: 'abc-123' }
↓
useQuery({
  queryKey: ['task', 'abc-123'],         ← chave do cache
  queryFn: () => getTaskById('abc-123'), ← função que busca
})
↓
PRIMEIRA vez: cache miss → chama queryFn
SEGUNDA vez: cache hit → retorna do cache sem request

PASSO 3 (axios, ~3ms)
apiClient.get('/api/tasks/abc-123')
↓
Adiciona baseURL → GET http://localhost:5000/api/tasks/abc-123
Adiciona header Content-Type: application/json

PASSO 4 (HTTP, ~20-100ms)
Request sai do navegador, viaja pela rede local,
chega no Kestrel (servidor HTTP do ASP.NET)

PASSO 5 (ASP.NET pipeline, ~3ms)
Kestrel recebe a request
↓
Middlewares rodam em ordem:
  UseCors         → confere Origin: http://localhost:5173 → ✅ passa
  UseHttpsRedir   → confere se precisa redirecionar → ✅ OK
  UseAuthorization → sem auth nesse projeto, skip
↓
 ControllerDispatcher acha TasksController
  → método GetById(Guid id) recebe id='abc-123'

PASSO 6 (Controller, ~1ms)
Cria query: new GetTaskByIdQuery(new TaskItemId(id))
↓
Chama handler.HandleAsync(query, ct) via DI

PASSO 7 (Application, ~3ms)
GetTaskByIdHandler:
  1. Pega ITaskRepository do DI
  2. Chama repo.GetByIdAsync(taskId)
  3. Devolve Result.Ok(task) ou Result.Fail(...)

PASSO 8 (Infrastructure, ~1ms)
InMemoryTaskRepository:
  1. Procura num Dictionary<Guid, TaskItem>
  2. Retorna a TaskItem encontrada (ou null)
  → Aqui, em produção, seria SQL/Cosmos:
     SELECT * FROM Tasks WHERE id = @id

PASSO 9 (Domain, ~0ms)
Devolve a TaskItem (que já passou por todas as invariantes
na criação, tá num estado válido por construção)

PASSO 10 (Controller, ~2ms)
Mapeia TaskItem (Domain) → TaskResponse (DTO)
  via extension method TaskItem.ToResponse()
  → esconde detalhes internos, controla o shape do JSON
↓
201 Created com Location header apontando pro recurso

PASSO 11 (volta pelo mesmo caminho)
JSON serializa → response HTTP → axios recebe → TanStack
Query guarda no cache com chave ['task', 'abc-123']
↓
React re-renderiza com a data nova
```

**Total: ~30-100ms** (a maior parte é ida e volta HTTP)

---

<a id="mutations"></a>
## Mutations — quando algo muda

**Cenário:** usuário clica em "Concluir" no detalhe.

```
PASSO 1 (React, ~1ms)
Usuário clica <Button onClick={() => concludeMutation.mutate()}>
↓
concludeMutation é um useMutation({ mutationFn: concludeTask })

PASSO 2 (axios, ~3ms)
apiClient.post(`/api/tasks/${id}/conclude`)

PASSO 3 (HTTP, ~30ms)
POST http://localhost:5000/api/tasks/abc-123/conclude

PASSO 4 (ASP.NET → Controller, ~5ms)
TasksController.Conclude(Guid id)
↓
Cria command: new ConcludeTaskCommand(new TaskItemId(id))
↓
Chama ConcludeTaskHandler.HandleAsync

PASSO 5 (Application, ~5ms)
ConcludeTaskHandler:
  1. repo.GetByIdAsync(taskId)           ← lê
  2. task.Conclude()                      ← chama Domain
  3. dispatcher.DispatchEvents(task.DomainEvents) ← dispara eventos
  4. uow.CommitAsync()                    ← salva
  5. devolve Result.Ok

PASSO 6 (Domain — o coração, ~1ms)
TaskItem.Conclude():
  if (Assignee is null) return Fail       ← INVARIANTE
  if (Status == Concluded) return Fail     ← INVARIANTE
  Status = Concluded                       ← muda estado
  ConcludedAt = DateTime.UtcNow            ← registra timestamp
  AddDomainEvent(new TaskConcludedEvent)  ← fala o que aconteceu

PASSO 7 (Infrastructure, ~2ms)
LoggingDispatcher:
  pega o evento → escreve no log "Tarefa abc-123 concluída às 14:30"
  → poderia mandar email, push notification, etc. MUDEI HOJE.

InMemoryUnitOfWork:
  marca como "salvo" (não precisa fazer nada em memória)

PASSO 8 (volta)
Result.Ok() volta pro Handler → Controller → 204 No Content

PASSO 9 (Frontend)
mutation.isSuccess → onSuccess hook:
  queryClient.invalidateQueries({ queryKey: ['task', id] })
  queryClient.invalidateQueries({ queryKey: ['tasks'] })
↓
TanStack Query revalida AS DUAS chaves
→ a lista recarrega (chip muda pra "Concluída")
→ o detalhe recarrega (botão "Concluir" desaparece)

PASSO 10 (UI atualiza, ~5ms)
React re-renderiza os 2 lugares. UI consistente.
```

---

<a id="caminhos-paralelos"></a>
## Caminhos paralelos — o que NÃO se fala

Algumas coisas que parecem "magia" mas só são setup one-time:

| O que | Onde mora | Quando roda |
|---|---|---|
| **Tema M2 customizado** | `src/theme/theme.ts` | Na inicialização do app (main.tsx) |
| **DI container do ASP.NET** | `Program.cs` + `Tasks.Application/DependencyInjection.cs` | No startup do servidor |
| **JSON enum converter** | `Program.cs` | Em cada serialize/deserialize |
| **CORS policy** | `Program.cs` (`AddCors`) | Em cada request |
| **QueryClient config** | `src/main.tsx` | Na inicialização |

Quando o Andre perguntar "como o backend sabe qual implementação de repo usar?",
a resposta é: **DI (Dependency Injection)**. Configura em
`Tasks.Application/DependencyInjection.cs` e `Tasks.Infrastructure/DependencyInjection.cs`,
e o ASP.NET resolve na hora.

---

<a id="roteiro-andre"></a>
## 🎯 Roteiro pra call com o Andre (15min)

### Antes da call (5min de preparação)

Tenha **aberto em outras abas** (pra não perder tempo procurando):

- `https://github.com/0xknob/task-app` — repo do back
- `https://github.com/0xknob/task-app-web` — repo do front
- `docs/learning-journal/01-a-viagem-de-3-semanas.md` — diário (se ele pedir contexto)
- `docs/adr/0001-accept-enums-as-strings.md` — exemplo de ADR
- `docs/learning-journal/02-como-as-pecas-se-conectam.md` — esse doc

**Backend rodando** na 5000. **Frontend rodando** na 5173. **Storybook**
(opcional, 6006) pra mostrar componentes.

### Roteiro (15min, cronometrado mentalmente)

#### Minuto 0-2: Abertura (contexto rápido)

> "Construí um microsserviço fullstack em 3 semanas partindo do zero em
> .NET. Tá dividido em 2 repos no GitHub. **Backend é a estrela** — é onde
> DDD e CQRS ficam visíveis. Front é o consumidor."

Mostra `https://github.com/0xknob/task-app` na aba principal.

#### Minuto 2-5: A pirâmide de dependência (o ponto-chave técnico)

> "O Domain é puro — não importa nada. Application depende só do Domain.
> Infrastructure implementa interfaces do Domain. Api é fina."

**Demonstre:** abra `Tasks.Domain/TaskAggregate/TaskItem.cs` e mostre
que os `using` são todos internos (sem `using Microsoft.EntityFramework` ou
`using System.Net.Http`). **Sem referência externa.**

**Mostre:** abra `Tasks.Application/Abstractions/ITaskRepository.cs` —
a interface que o Domain define, que a Infrastructure implementa.

#### Minuto 5-8: Invariantes (a regra que me deu trabalho)

> "Cada entidade tem invariantes testadas. Exemplo: concluir tarefa sem
> assignee é proibido. 69 testes xUnit cobrem isso."

**Demonstre:**

1. Abra `Tasks.Domain.Tests/TaskAggregate/TaskItemConcludeTests.cs`
2. Mostra um teste:
   ```csharp
   [Fact]
   public void Conclude_WithoutAssignee_ShouldFail()
   {
       var task = TaskItem.Create(...);
       var result = task.Value.Conclude();
       Assert.True(result.IsFailure);
   }
   ```
3. Roda `dotnet test` no terminal — **69 passando em 2 segundos**

#### Minuto 8-10: CQRS em ação (separação)

> "Commands mudam estado. Queries só leem. Handlers diferentes, INTENÇÕES diferentes."

**Demonstre:**

1. Abre `Tasks.Application/Tasks/Commands/ConcludeTask/ConcludeTaskHandler.cs`
   — vê: lê, chama domínio, despacha eventos, commita
2. Abre `Tasks.Application/Tasks/Queries/GetTaskById/GetTaskByIdHandler.cs`
   — vê: lê, devolve. Sem commit, sem eventos.

#### Minuto 10-12: Documentação (o diferencial)

> "Decisões arquiteturais ficam registradas em ADR. Esse aqui é sobre
> aceitar enums como string no JSON."

**Demonstre:** abre `docs/adr/0001-accept-enums-as-strings.md` e lê o
header + 3 bullets do "decisão".

> "Tem também um diário de aprendizado em `docs/learning-journal/` que
> descreve como foi cada dia."

#### Minuto 12-14: A troca de persistência (mostra maturidade)

> "Hoje a persistência é InMemory. Pra trocar por SQL ou Cosmos, **só
> Infrastructure muda**. Os Handlers nem sabem."

**Demonstre:** mostra `Tasks.Infrastructure/DependencyInjection.cs`:

```csharp
services.AddScoped<ITaskRepository, InMemoryTaskRepository>();
```

> "Trocar essa linha é o suficiente. O Domain, Application e API ficam
> intactos. Por isso o esqueleto escala."

#### Minuto 14-15: Demonstração ao vivo (ou vídeo)

Se der tempo, **mostra a app rodando**:

1. Abre `http://localhost:5173` — lista carrega do backend
2. Clica numa tarefa — detalhe abre
3. Clica "Concluir" — chip muda, volta pra lista atualizada
4. (Bônus) Abre Storybook `http://localhost:6006` — mostra o `TaskCard` com6 stories

### O que **NÃO** falar (auto-sabotagem)

- ❌ "Falta muita coisa" / "Ainda tô aprendendo" / "É simplesinho"
- ❌ Listar TUDO que tá no roadmap como não-feito
- ❌ Defender o que não foi feito (Azure deploy, auth) — admite e segue
- ❌ Falar mais de 1min sem mostrar código ou rodar algo
- ❌ Prometer que faz em X semanas sem perguntar

### O que **SIM** falar (auto-promoção honesta)

- ✅ "O que tá sólido é o esqueleto DDD + CQRS + testes. Sei explicar cada decisão."
- ✅ "Já passei pelo ciclo completo: modelei, testei, documentei, integrei."
- ✅ "Tenho um diário documentando o que aprendi em cada dia."
- ✅ "Se eu tivesse que recumir amanhã, faria diferente em X e Y" (reflexão)
- ✅ "Quero aprender Azure deploy, Cosmos DB, e IA aplicada a desenvolvimento"

### Perguntas que o Andre **vai** fazer (preparar respostas)

| Pergunta provável | Resposta curta |
|---|---|
| "Por que DDD e não só MVC?" | "Porque a regra de negócio é complexa o suficiente pra precisar de invariantes explícitas. DDD me forçou a colocar 'não conclui sem assignee' num lugar só, testado." |
| "Por que CQRS se é mais código?" | "Porque separar Commands de Queries deixa cada handler com UMA responsabilidade. Testar fica trivial. E se um dia eu precisar de read replica, já tá separado." |
| "Por que InMemory e não SQL?" | "Proposital. Pra mostrar que **trocar a persistência não exige mudar nada acima**. O contrato é a interface. Quando eu plugar SQL, os handlers ficam idênticos." |
| "Como você testa invariantes?" | "xUnit no Domain. Cada invariante vira 2 testes: o caminho feliz e o caminho que viola a regra. 69 testes, ~2 segundos pra rodar." |
| "Como você documenta decisão?" | "ADR. Cada escolha arquitetural vira um arquivo curto (contexto, decisão, consequências, alternativas rejeitadas)." |
| "O que falta?" | "Azure deploy real, Cosmos DB no lugar de InMemory, CI/CD, logging estruturado, autenticação. Tá no roadmap do README." |

### Pergunta que **você** faz pro Andre (final da call)

Tenha **uma** pergunta pronta. Sugestões:

> "Como você começou com .NET? Tem alguma referência / livro que você
> recomenda pra além do que a documentação oficial cobre?"

OU

> "No time de vocês, como vocês decidem entre Entity Framework e Dapper
> pra um projeto novo? Tem critério?"

OU (a mais segura, se o tempo tiver curto)

> "Tem alguma parte do código que você abriria e mostraria como deveria
> ter feito?"

---

## Checklist 5min antes da call

- [ ] Backend rodando (`dotnet run` em `Tasks.Api`)
- [ ] Frontend rodando (`npm run dev` em `task-app-web`)
- [ ] Storybook aberto (opcional, `npm run storybook`)
- [ ] GitHub aberto em 2 abas (front + back)
- [ ] ADR-0001 aberto em outra aba
- [ ] Diário aberto em outra aba (se ele quiser contexto)
- [ ] TaskListPage mostra umas5-6 tarefas (use `seed-demo.py` se vazio)
- [ ] 1 tarefa tem comentários (pra mostrar a UI completa no detalhe)
- [ ] `dotnet test` rodou limpo (pra garantir que os 69 testes passam **agora**)

---

*Boa call. Lembre: ele não tá te testando, tá te conhecendo. Sê curioso,
admite o que não sabe, mostra o que sabe fazer.*