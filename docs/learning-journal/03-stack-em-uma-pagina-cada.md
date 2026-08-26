# Stack em Uma Página Cada — Reference Card

> Cada tecnologia do projeto, explicada em 1-2 páginas. **Sem fluff.**
> Explica o que é, **por que** está no projeto, e **um snippet real do nosso código**.
>
> **Como usar:** se você esqueceu como algo funciona durante a call, abre aqui.
> Se o Andre perguntar "por que X?", o snippet responde.

---

## Índice

- [.NET 10 + C# 13](#dotnet)
- [DDD tático (Domain-Driven Design)](#ddd)
- [CQRS (Command Query Responsibility Segregation)](#cqrs)
- [xUnit (testes)](#xunit)
- [ASP.NET Core (webapi)](#aspnet)
- [Repository Pattern + Unit of Work](#repo-uow)
- [Result Pattern](#result)
- [React 19](#react)
- [TypeScript 6](#typescript)
- [TanStack Query](#tanstack)
- [React Hook Form + Zod](#rhf-zod)
- [Material UI + Material Design M2](#mui)
- [Storybook](#storybook)
- [Vite 8](#vite)
- [CORS](#cors)

---

<a id="dotnet"></a>
## .NET 10 + C# 13

**O que é:** plataforma de desenvolvimento da Microsoft. Compila pra um
runtime (CLR) que roda em Windows, Linux, macOS. C# é a linguagem.

**Por que no projeto:** padrão da empresa-alvo. Tem ecossistema gigante,
performance boa, e o tooling é dos melhores (VS Code, Rider, Visual Studio).

**Características que usamos no projeto:**

```csharp
// Records (imutáveis, igualdade por valor)
public sealed record TaskItemId(Guid Value);

// Nullable reference types (análise estática de null)
public string Title { get; private set; } = default!;

// Pattern matching
return Status switch
{
    TaskStatus.Pending => "Pendente",
    TaskStatus.InProgress => "Em progresso",
    TaskStatus.Concluded => "Concluída",
    _ => "?"
};

// Async/await (não bloqueia thread)
public async Task<Result<TaskItem>> HandleAsync(CreateTaskCommand cmd, CancellationToken ct)
{
    var task = await repo.GetByIdAsync(cmd.Id, ct);
    return Result.Ok(task);
}

// Primary constructors (C# 12+)
public class CreateTaskHandler(ITaskRepository repo, IUnitOfWork uow)
{
    // repo e uow viraram campos privados automaticamente
}
```

**Arquivos centrais:** `Tasks.Domain/TaskAggregate/TaskItem.cs`,
`Tasks.Application/Tasks/Commands/CreateTask/CreateTaskHandler.cs`.

---

<a id="ddd"></a>
## DDD tático (Domain-Driven Design)

**O que é:** abordagem pra modelar software onde **a estrutura do código
segue o domínio do negócio**. Domínio = as regras e conceitos que os
especialistas do assunto usam.

**Por que no projeto:** o briefing da vaga pede DDD. E o problema
(regras de "tarefa precisa de assignee", "prazo não pode ser no passado")
é exatamente o tipo de coisa que DDD resolve bem.

**Os blocos que usamos:**

| Bloco | O que é | No nosso código |
|---|---|---|
| **Aggregate** | Cluster de objetos tratados como unidade | `TaskItem` |
| **Aggregate Root** | Porta de entrada do agregado | `TaskItem` (mesma classe) |
| **Entity** | Objeto com identidade | `TaskItem`, `Comment` |
| **Value Object** | Objeto sem identidade, definido pelo valor | `Title`, `Description`, `DueDate` |
| **Domain Event** | Fato do passado que aconteceu | `TaskConcludedEvent` |
| **Repository** | Abstração de persistência (interface no Domain) | `ITaskRepository` |

**A regra de ouro:**

> **Domain não conhece nada acima dele.** Se você ver `using Tasks.Infrastructure`
> em algum arquivo do Domain, tem algo errado.

---

<a id="cqrs"></a>
## CQRS (Command Query Responsibility Segregation)

**O que é:** separar **operações de mudança** (Commands) das **operações de leitura** (Queries). Cada uma tem seu handler.

**Por que no projeto:** a vaga pede. E ajuda muito no raciocínio — um
handler de Command tem "carregar, validar, mutar, salvar, despachar eventos".
Um de Query tem só "carregar, devolver". Cada um é simples.

**Estrutura de pastas:**

```
Tasks.Application/Tasks/
├── Commands/
│   ├── CreateTask/
│   │   ├── CreateTaskCommand.cs       ← intent (record)
│   │   └── CreateTaskHandler.cs      ← implementação
│   ├── ConcludeTask/
│   ├── AssignTask/
│   └── AddComment/
└── Queries/
    ├── GetTaskById/
    └── ListTasks/
```

**Exemplo real (Command):**

```csharp
public record CreateTaskCommand(
    string Title,
    string Description,
    Priority Priority,
    DateTime DueDate);

public class CreateTaskHandler : IRequestHandler<CreateTaskCommand, Result<TaskItemId>>
{
    public async Task<Result<TaskItemId>> HandleAsync(CreateTaskCommand cmd, CancellationToken ct)
    {
        var taskResult = TaskItem.Create(cmd.Title, cmd.Description, cmd.Priority, cmd.DueDate);
        if (taskResult.IsFailure) return Result.Fail<TaskItemId>(taskResult.Error!);

        var task = taskResult.Value!;
        await _repo.AddAsync(task, ct);
        await _uow.CommitAsync(ct);
        _dispatcher.DispatchEvents(task.DomainEvents);
        return Result.Ok(task.Id);
    }
}
```

**Exemplo real (Query):**

```csharp
public class GetTaskByIdHandler
{
    public async Task<Result<TaskItem>> HandleAsync(GetTaskByIdQuery query, CancellationToken ct)
    {
        var task = await _repo.GetByIdAsync(query.Id, ct);
        if (task is null) return Result.Fail<TaskItem>("Tarefa não encontrada.");
        return Result.Ok(task);
    }
}
```

Note como a Query **não chama uow.CommitAsync** nem **dispatcha eventos**.
Só lê. CQRS em ação.

---

<a id="xunit"></a>
## xUnit (testes)

**O que é:** framework de testes mais popular do ecossistema .NET.

**Por que no projeto:** testar invariantes é **obrigatório** em DDD.
Sem teste, ninguém garante que a regra de negócio tá sendo respeitada.

**Exemplo real (Domain):**

```csharp
public class TaskItemConcludeTests
{
    [Fact]
    public void Conclude_WithAssignee_ShouldSucceed()
    {
        // Arrange
        var taskResult = TaskItem.Create(
            "Estudar DDD",
            "Ler livro azul",
            Priority.High,
            DateTime.UtcNow.AddDays(7));
        var task = taskResult.Value!;
        task.AssignTo(Guid.NewGuid());

        // Act
        var result = task.Conclude();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TaskStatus.Concluded, task.Status);
    }

    [Fact]
    public void Conclude_WithoutAssignee_ShouldFail()
    {
        // Arrange
        var taskResult = TaskItem.Create("Estudar DDD", "x", Priority.High, DateTime.UtcNow.AddDays(7));

        // Act
        var result = taskResult.Value!.Conclude();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("atribuída", result.Error);
    }
}
```

**Padrão AAA:** Arrange (prepara), Act (executa), Assert (verifica).
Fica óbvio o que cada teste tá garantindo.

**Comando:** `dotnet test` → roda todos os 69 testes em ~2s.

---

<a id="aspnet"></a>
## ASP.NET Core (webapi)

**O que é:** framework da Microsoft pra construir APIs HTTP e apps web.
Lida com roteamento, middlewares, autenticação, serialização JSON, etc.

**Por que no projeto:** backend é API REST. ASP.NET Core é o padrão .NET.

**Pipeline de middlewares (ordem importa!):**

```csharp
var app = builder.Build();

app.UseCors("DevFront");           // 1. CORS check
app.UseHttpsRedirection();        // 2. Redireciona HTTP → HTTPS
app.UseAuthorization();            // 3. Auth (vazio nesse projeto)
app.MapControllers();              // 4. Roteia pra controller
```

Cada middleware roda em ordem. Cada um pode **bloquear** ou **transformar**
a request antes de passar pro próximo.

**Controller é fino (só tradução HTTP ↔ Application):**

```csharp
[HttpPost]
public async Task<IActionResult> Create(
    [FromBody] CreateTaskRequest request,
    [FromServices] CreateTaskHandler handler,
    CancellationToken ct)
{
    var command = new CreateTaskCommand(...);
    var result = await handler.HandleAsync(command, ct);

    if (result.IsFailure) return BadRequest(new { error = result.Error });
    return CreatedAtAction(nameof(GetById), new { id = result.Value!.TaskId.Value }, ...);
}
```

**SEM regra de negócio aqui.** Controller é tradutor, não decisor.

---

<a id="repo-uow"></a>
## Repository Pattern + Unit of Work

**O que é:** abstrair persistência. Domain define interface; Infrastructure
implementa. UoW garante transação atômica.

**Por que no projeto:** se o InMemory virar SQL amanhã, **só a implementação
muda**. Handlers nem sabem.

**Domain define o contrato:**

```csharp
// Tasks.Domain/TaskAggregate/ITaskRepository.cs
public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(TaskItemId id, CancellationToken ct);
    Task AddAsync(TaskItem task, CancellationToken ct);
    // ...
}
```

**Infrastructure implementa:**

```csharp
// Tasks.Infrastructure/Persistence/InMemory/InMemoryTaskRepository.cs
public class InMemoryTaskRepository : ITaskRepository
{
    private readonly Dictionary<TaskItemId, TaskItem> _store = new();

    public Task<TaskItem?> GetByIdAsync(TaskItemId id, CancellationToken ct)
    {
        _store.TryGetValue(id, out var task);
        return Task.FromResult(task);
    }
    // ...
}
```

**Trocar por SQL amanhã:**

```csharp
// TAREFA: criar SqlTaskRepository : ITaskRepository
// Trocar no DI: services.AddScoped<ITaskRepository, SqlTaskRepository>();
// Handlers, Controllers, Domain: ZERO mudança.
```

**Unit of Work:** coordena múltiplos repositórios numa transação só.

```csharp
await repo.AddAsync(task);
await otherRepo.AddAsync(comment);
await uow.CommitAsync();  // commit atômico
```

---

<a id="result"></a>
## Result Pattern

**O que é:** tipo que representa sucesso **ou** falha com mensagem, em vez
de lançar exception.

**Por que no projeto:** validações de entrada são **erros esperados**, não
excepcionais. Usar exception pra "título vazio" é caro computacionalmente
e semanticamente errado.

**Implementação:**

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? Error { get; }

    public static Result<T> Ok(T value) => new(true, value, null);
    public static Result<T> Fail(string error) => new(false, default, error);
}

public class Result  // versão sem valor
{
    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);
}
```

**Uso:**

```csharp
public Result<TaskItem> Create(string title)
{
    if (string.IsNullOrWhiteSpace(title))
        return Result.Fail<TaskItem>("Título vazio");
    return Result.Ok(new TaskItem(title));
}

// Caller
var result = TaskItem.Create("x", "y", Priority.High, dueDate);
if (result.IsFailure)
    return BadRequest(result.Error);
var task = result.Value!;
```

---

<a id="react"></a>
## React 19

**O que é:** biblioteca pra construir UIs baseada em componentes. Tudo é
função que retorna JSX (parece HTML mas é JavaScript).

**Por que no projeto:** padrão de mercado. Ecossistema gigante, fácil
encontrar devs, fácil encontrar libs.

**Conceitos que usamos:**

```tsx
// Componente funcional
function TaskListPage() {
  // Hook de estado local
  const [filter, setFilter] = useState('all');

  // Hook de server-state (TanStack Query)
  const { data, isPending } = useQuery({
    queryKey: ['tasks', filter],
    queryFn: () => getTasks({ status: filter }),
  });

  // JSX = parece HTML
  return (
    <Box>
      {data?.map(task => <TaskCard key={task.id} task={task} />)}
    </Box>
  );
}
```

**Hooks principais que usamos:**

| Hook | Pra quê |
|---|---|
| `useState` | Estado local |
| `useMemo` | Memoriza valor (evita recálculo) |
| `useQuery` | Busca/cache HTTP (TanStack) |
| `useMutation` | POST/PUT/DELETE (TanStack) |
| `useForm` | Estado de formulário (RHF) |
| `useParams` | Captura params da URL (React Router) |
| `useNavigate` | Navegação programática (React Router) |

---

<a id="typescript"></a>
## TypeScript 6

**O que é:** superset do JavaScript que adiciona tipos estáticos.

**Por que no projeto:** erra em vez de surpresar. Refatoração segura.
Documentação viva (o tipo diz o que a função espera).

**Como usamos:**

```typescript
// Interface pra objeto
interface Task {
  id: string;
  title: string;
  status: 'Pending' | 'InProgress' | 'Concluded';  // string union
  priority: 'Low' | 'Medium' | 'High';
  dueDate: string;
  // ...
}

// Tipo pra função
type StatusFilter = 'All' | 'Pending' | 'InProgress' | 'Concluded';

// Generics
async function getTasks<T>(params?: T): Promise<Task[]> { ... }

// Inferência do Zod
const taskSchema = z.object({ title: z.string() });
type Task = z.infer<typeof taskSchema>;  // TS infere da schema
```

**Conceito chave:** `type X = ...` vs `interface X { ... }` — quase a
mesma coisa. `type` pra unions/intersections. `interface` pra objetos
(declaration merging).

---

<a id="tanstack"></a>
## TanStack Query

**O que é:** biblioteca pra **server-state** (cache de dados que vêm do
servidor). Substitui Redux pra 90% dos casos.

**Por que no projeto:** cache, refetch, retry, invalidação por chave. Você
não precisa reinventar isso.

**Conceitos centrais:**

```typescript
// QUERY (GET) — busca dados
const { data, isPending, isError } = useQuery({
  queryKey: ['tasks', filter],        // chave do cache
  queryFn: () => getTasks({ status: filter }),
});

// MUTATION (POST/PUT/DELETE) — muda dados
const mutation = useMutation({
  mutationFn: createTask,
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['tasks'] });
  },
});

// Chamar: mutation.mutate(payload)
```

**Cache por chave:** `['tasks']`, `['tasks', { status: 'Pending' }]`,
`['task', id]`. Cada chave é um cache separado. Quando você invalida,
TanStack refaz o fetch.

**Quando usar / não usar:**

| Usar | Não usar |
|---|---|
| Cache de HTTP | State global da app (tema, modal aberto) |
| Refetch automático | State de formulário (use RHF) |
| Retry com backoff | Validação (use Zod) |

---

<a id="rhf-zod"></a>
## React Hook Form + Zod

**O que é:** dupla de libs pra forms. RHF gerencia estado; Zod valida.

**Por que no projeto:** forms sem re-render a cada tecla (RHF). Validação
tipada e compartilhada com backend (Zod).

**Como usamos juntos:**

```typescript
// Schema de validação = contrato + tipo TypeScript
const createTaskSchema = z.object({
  title: z.string().min(3, 'mínimo 3 chars'),
  description: z.string().max(2000),
  priority: z.enum(['Low', 'Medium', 'High']),
  dueDate: z.string().min(1, 'obrigatório'),
});

type CreateTaskForm = z.infer<typeof createTaskSchema>;

// RHF + Zod juntos
const { control, handleSubmit, formState: { errors } } = useForm<CreateTaskForm>({
  resolver: zodResolver(createTaskSchema),
  defaultValues: { title: '', description: '', priority: 'Medium', dueDate: '' },
});

// Render com Controller
<Controller
  name="title"
  control={control}
  render={({ field }) => (
    <TextField
      {...field}
      label="Título"
      error={!!errors.title}
      helperText={errors.title?.message}
    />
  )}
/>
```

**Fluxo:** usuário digita → RHF atualiza estado SEM re-renderizar tudo →
submit → Zod valida → se passar, mutation dispara → API.

---

<a id="mui"></a>
## Material UI + Material Design M2

**O que é:** Material UI (MUI) é a lib de componentes React baseada em
Material Design (linguagem visual do Google). Material Design tem 2
versões principais: **M2** (2014) e **M3** (2021).

**Por que no projeto:** a empresa-alvo usa M2. MUI por padrão é M3, então
**customizamos o tema**.

**Como customizamos:**

```typescript
// src/theme/theme.ts
export const theme = createTheme({
  palette: {
    primary: { main: '#1976d2' },  // azul M2 (não o M3 arroxeado)
    success: { main: '#2e7d32' },  // verde
    warning: { main: '#ed6c02' },  // laranja
  },
  shape: { borderRadius: 4 },       // M2 = 4px, M3 = mais arredondado
  typography: {
    fontFamily: '"Roboto", "Helvetica", "Arial", sans-serif',
    button: { textTransform: 'none' },  // M2 NÃO força CAIXA ALTA
  },
});

// Aplicar no app
<ThemeProvider theme={theme}>
  <CssBaseline />
  <App />
</ThemeProvider>
```

**Componentes principais:**

| Componente | Pra que |
|---|---|
| `Box` | Wrapper flexbox (substitui div) |
| `Stack` | Layout vertical/horizontal com spacing |
| `Paper` | Card com elevação |
| `Chip` | Badge pequena (status, tags) |
| `Card` + `CardActionArea` | Card clicável |
| `Fab` | Floating action button (ação primária) |

---

<a id="storybook"></a>
## Storybook

**O que é:** playground pra componentes isolados. Roda em porta separada
(6006 por padrão).

**Por que no projeto:** designer/PO consegue revisar visual sem rodar a
app. Cada componente tem stories que cobrem estados visuais diferentes.

**Como escrevemos:**

```typescript
// src/components/TaskStatusChip.stories.tsx
import type { Meta, StoryObj } from '@storybook/react-vite';
import { TaskStatusChip } from './TaskStatusChip';

const meta = {
  title: 'Components/TaskStatusChip',
  component: TaskStatusChip,
  argTypes: {
    status: { control: { type: 'select' }, options: ['Pending', 'InProgress', 'Concluded'] },
  },
} satisfies Meta<typeof TaskStatusChip>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Pending: Story = { args: { status: 'Pending' } };
export const InProgress: Story = { args: { status: 'InProgress' } };
export const Concluded: Story = { args: { status: 'Concluded' } };

export const AllVariants: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: 8 }}>
      <TaskStatusChip status="Pending" />
      <TaskStatusChip status="InProgress" />
      <TaskStatusChip status="Concluded" />
    </div>
  ),
};
```

**Comandos:** `npm run storybook` (abre em :6006), `npm run build-storybook` (build estático).

---

<a id="vite"></a>
## Vite 8

**O que é:** build tool moderno. Substituiu Webpack/CRA. Usa esbuild
internamente pra ser rápido.

**Por que no projeto:** HMR instantâneo. Build em milissegundos. Padrão
atual da indústria.

**Configuração no projeto:**

```typescript
// vite.config.ts
export default defineConfig({
  plugins: [react()],
});
```

**Comandos:**

| Comando | O que faz |
|---|---|
| `npm run dev` | Sobe dev server com HMR em :5173 |
| `npm run build` | Build produção em `dist/` |
| `npm run preview` | Serve o `dist/` localmente pra teste |
| `npm run storybook` | Sobe Storybook em :6006 |

---

<a id="cors"></a>
## CORS

**O que é:** Cross-Origin Resource Sharing. Política de segurança do
**navegador** (não do servidor!) que bloqueia requests entre origens
diferentes por padrão.

**Por que tá no projeto:** front (`:5173`) e back (`:5000`) são origens
diferentes. Sem CORS configurado, navegador bloqueia tudo.

**Como configuramos:**

```csharp
// Tasks.Api/Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevFront", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:6006")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();
app.UseCors("DevFront");  // ANTES de tudo
```

**Por que devFront:** em produção (Azure), a origin vai mudar. Vai precisar
atualizar essa lista.

**Sintoma clássico:** console do navegador mostra:
```
Access to XMLHttpRequest at 'http://localhost:5000/api/tasks' from origin
'http://localhost:5173' has been blocked by CORS policy
```

---

## Como usar essa referência

| Situação | Onde olhar |
|---|---|
| Esquceu como CORS funciona | [Seção CORS](#cors) |
| Andre perguntou sobre CQRS | [Seção CQRS](#cqrs) |
| Precisa lembrar como usar Zod | [Seção RHF+Zod](#rhf-zod) |
| Vai explicar pirâmide DDD | [Seção DDD](#ddd) |
| Não lembra ordem dos middlewares | [Seção ASP.NET](#aspnet) |
| Quer ver exemplo de teste | [Seção xUnit](#xunit) |