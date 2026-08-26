# A Viagem de 3 Semanas — Diário de Construção do task-app

> Documento escrito em primeira pessoa pelo autor do projeto (um dev júnior
> em formação), descrevendo como cada peça foi construída, **por que** foi
> assim, e o que deu errado no caminho.
>
> **Pra quem é esse texto?** Pra devs juniores que vão abrir o repo e
> querem entender o raciocínio por trás do código, não só o código.
> Pra entrevistadores que querem ver **processo de aprendizado**, não só
> produto final.
>
> **Como ler:** linear, do começo ao fim. Cada capítulo é ~1 dia de trabalho
> real (2-3h). Pode pular se já manja do assunto.

---

## Índice

1. [Dia 1 — Setup e o primeiro "Hello World" em C#](#dia-1)
2. [Dia 2 — O agregado TaskItem nasce (e os bugs)](#dia-2)
3. [Dia 3 — Frontend toma forma (React + MUI)](#dia-3)
4. [Dia 4 — Integração end-to-end e o pesadelo do CORS](#dia-4)
5. [Dia 5 — Formulário, validação e o bug da data](#dia-5)
6. [Dia 6 — Componentes reutilizáveis + Storybook](#dia-6)
7. [Dia 7 — Polimento, README honesto e o LinkedIn](#dia-7)
8. [Apêndice — Padrões que aprendi nesse caminho](#apêndice)

---

## Antes de começar: o que eu já sabia vs o que eu não sabia

| Sabia | Não sabia |
|---|---|
| Git básico (commit, branch, push) | .NET, C#, DDD, CQRS |
| React com hooks | Material Design 2 (só conhecia M3 por alto) |
| TypeScript intermediário | Storybook, TanStack Query, Zod |
| HTML/CSS | Azure, Cosmos DB, Terraform |
| Conceito de API REST | CORS na prática (só sabia o nome) |

**O plano:** construir algo real em 30 dias pra mostrar que aprendo rápido
e que sei tomar decisões técnicas. O escopo é ger um um (gestão de tarefas) —
o **esqueleto** é o produto, o domínio é só a desculpa pra provar que ele
funciona.

---

<a id="dia-1"></a>
## Dia 1 — Setup e o primeiro "Hello World" em C#

### O que eu fiz

Instalei tudo: **.NET 10 SDK**, **Node 24**, **VS Code**, **Git**.
Criei a solution .NET com 4 projetos separados (essa é a parte de DDD):

```
Tasks.sln
├── Tasks.Domain        (classlib puro, SEM dependências externas)
├── Tasks.Application   (classlib)
├── Tasks.Infrastructure (classlib)
└── Tasks.Api           (webapi)
```

### Por que 4 projetos?

Aprendi no dia anterior que **DDD tático** separa o código em camadas
"puras" e camadas "impuras". A regra de ouro é:

> **Domain não conhece ninguém acima dele. Application depende só do Domain.
> Infrastructure implementa interfaces. Api é fina.**

Isso vira uma **pirâmide de dependências**:

```
        Api  ─────────┐
                      │ depende de todos
        Infrastructure │
                      │ depende de Application
        Application   │
                      │ depende SÓ de Domain
        Domain        │ (não depende de ninguém)
```

Por que isso importa? Porque se você quiser trocar o banco de InMemory
por SQL Server, **só Infrastructure muda**. Os handlers em Application
nem ficam sabendo.

### Comandos que rodaram

```bash
dotnet new sln -n TasksApp
dotnet new classlib -n Tasks.Domain -o src/Tasks.Domain
dotnet new classlib -n Tasks.Application -o src/Tasks.Application
dotnet new classlib -n Tasks.Infrastructure -o src/Tasks.Infrastructure
dotnet new webapi -n Tasks.Api -o src/Tasks.Api
dotnet sln add src/*/*.csproj
dotnet add src/Tasks.Application reference src/Tasks.Domain/Tasks.Domain.csproj
dotnet add src/Tasks.Infrastructure reference src/Tasks.Application/Tasks.Application.csproj
dotnet add src/Tasks.Api reference src/Tasks.Infrastructure/Tasks.Infrastructure.csproj
```

O truque do `reference` é: cada projeto referencia quem está abaixo dele
na pirâmide. `Domain` não tem nenhum `reference`, então é puro mesmo.

### O que deu errado

Nada nessa etapa, foi bem mecânico. Mas eu demorei pra entender **a ordem
certa** dos `dotnet add reference` — se eu referenciasse Api em Domain
(direto), quebraria a regra DDD.

---

<a id="dia-2"></a>
## Dia 2 — O agregado TaskItem nasce

### O que eu tentei fazer

Construir a entidade central: uma tarefa. Parece simples ("título, descrição,
data"), mas DDD te força a pensar mais.

### O que é um "agregado"?

Aggregate é um **cluster de objetos** que você trata como **uma unidade só**.
"Pra mexer em qualquer parte, passa pela raiz". No nosso caso:

```
TaskItem (raiz do agregado)
├── Title (value object)
├── Description (value object)
├── DueDate (value object)
├── Priority (enum)
├── TaskStatus (enum)
├── Assignee (entidade interna, opcional)
└── Comments (lista de entidades internas)
```

A regra de ouro: **pra criar, ler, atualizar ou deletar qualquer coisa
dentro do agregado, você passa pela raiz**.

### Value Objects — pra que servem?

Value Objects são **objetos sem identidade própria**, definidos só pelo valor.
Dois `Title("Estudar DDD")` são o mesmo objeto, mesmo que sejam instâncias
diferentes.

Por que usar? **Validação no construtor.** Se você conseguiu instanciar um
`Title`, ele é válido. É impossível ter um Title vazio em memória porque o
construtor recusa.

```csharp
public sealed record Title
{
    public const int MaxLength = 200;
    public string Value { get; }

    private Title(string value) => Value = value;  // construtor PRIVADO

    public static Result<Title> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Fail<Title>("Título não pode ser vazio.");
        if (value.Length > MaxLength)
            return Result.Fail<Title>($"Máximo {MaxLength} caracteres.");
        return Result.Ok(new Title(value.Trim()));
    }
}
```

Note o **construtor privado** + **factory method** que devolve `Result<T>`
em vez de lançar exceção. Por quê?

### Result Pattern vs Exceptions

| Lançar exceção | Devolver Result |
|---|---|
| Cara computacional (stack trace) | Barata (struct) |
| Controle de fluxo escondido | Controle explícito |
| "Erro inesperado" | "Erro esperado" |

Em DDD, validações de entrada são **erros esperados**, não excepcionais.
Então usamos `Result<T>` pra sinalizar sucesso ou falha com mensagem.

### A invariante que me deu trabalho

A regra de negócio mais importante do nosso domínio:

> **"Não dá pra concluir uma tarefa sem assignee."**

Vira isso no código:

```csharp
public UnitResult Conclude()
{
    if (Assignee is null)
        return Result.Fail("Tarefa precisa estar atribuída antes de concluir.");
    if (Status == TaskStatus.Concluded)
        return Result.Fail("Tarefa já está concluída.");

    Status = TaskStatus.Concluded;
    ConcludedAt = DateTime.UtcNow;
    AddDomainEvent(new TaskConcludedEvent(Id, ConcludedAt.Value, DateTime.UtcNow));
    return Result.Ok();
}
```

### Domain Events — o agregado fala sem saber pra quem

Quando `Conclude()` roda, ele **emite um evento** (`TaskConcludedEvent`).
Quem vai tratar esse evento? Application, Infrastructure, ninguém — não importa.
O agregado só sabe que **isso é um fato do passado que aconteceu**.

Isso desacopla: amanhã, se você quiser mandar um email quando uma tarefa
for concluída, não mexe no Domain — só adiciona um handler do evento em
Application/Infrastructure.

### O que deu errado

**Bug 1**: escrevi `TaskStatus` como `int` em vez de enum. O compilador
aceitou (int é compatível), mas perdi a legibilidade. Voltei e fiz enum
correto.

**Bug 2**: esqueci o `private` no construtor. Aí qualquer um podia
instanciar `new TaskItem()` sem validação. Voltou a regra.

---

<a id="dia-3"></a>
## Dia 3 — Frontend toma forma

### O que eu fiz

Criei o projeto React com **Vite** (não Create React App — Vite é o padrão
atual e builda100× mais rápido). Instalei o stack todo:

```bash
npm create vite@latest task-app-web -- --template react-ts
cd task-app-web
npm install @mui/material @emotion/react @emotion/styled
npm install @tanstack/react-query axios react-router-dom
npm install -D @storybook/react-vite @storybook/addon-a11y
```

### Por que essas escolhas?

| Tecnologia | Por que |
|---|---|
| **Vite** | Build e HMR instantâneo. Padrão atual da indústria. |
| **MUI (Material UI)** | A empresa-alvo usa Material Design. MUI tem tema customizável. |
| **TanStack Query** | Cache HTTP sem Redux. Refetch, retry, invalidação automática. |
| **React Hook Form** | Forms sem re-render a cada tecla. Performático. |
| **Zod** | Schema de validação que vira tipo TypeScript. |
| **Storybook** | Cada componente documentado isoladamente. Designers/POs revisam. |

### Tema Material Design M2 (não M3)

Pequena pegadinha: **MUI usa M3 por padrão**, mas a empresa-alvo usa M2.
Solução: tema customizado sobrescrevendo tokens.

```typescript
// src/theme/theme.ts
export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: { main: '#1976d2' },  // azul M2 (não o M3 que é mais arroxeado)
  },
  shape: { borderRadius: 4 },  // M2 usa 4px, M3 usa cantos mais arredondados
  typography: {
    fontFamily: '"Roboto", "Helvetica", "Arial", sans-serif',
    button: { textTransform: 'none' },  // M2 NÃO força CAIXA ALTA
  },
});
```

### Os 3 providers que ficam ao redor de tudo

```typescript
// src/main.tsx
<ThemeProvider theme={theme}>
  <QueryClientProvider client={queryClient}>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </QueryClientProvider>
</ThemeProvider>
```

Cada Provider dá um "poder" pra tudo que está dentro:
- **ThemeProvider**: acesso ao tema M2 em qualquer componente
- **QueryClientProvider**: `useQuery` / `useMutation` funcionam
- **BrowserRouter**: rotas SPA (URL muda sem recarregar)

### O que deu errado

**Bug**: o Storybook não carregava os ícones. Resolvi instalando
`@mui/icons-material` separado — eles não vêm no `@mui/material`.

---

<a id="dia-4"></a>
## Dia 4 — Integração end-to-end e o pesadelo do CORS

### O que eu tentei

Fazer o frontend **de fato conversar** com o backend. Até então, eram dois
repos separados que não se falavam.

### O frontend precisa de uma camada de API

Não dá pra espalhar `axios.get('http://localhost:5000/api/tasks')` por todo
o código. Criei `src/api/tasks.ts`:

```typescript
export async function getTasks(): Promise<Task[]> {
  const response = await apiClient.get<Task[]>('/api/tasks');
  return response.data;
}
```

E `apiClient` em `src/api/client.ts`:

```typescript
import axios from 'axios';
export const apiClient = axios.create({
  baseURL: 'http://localhost:5000',
  timeout: 10000,
});
```

**Por que separar?** Se o endpoint mudar, mexe em 1 arquivo só. Se você
trocar axios por fetch, mexe em 1 arquivo só. **Single Responsibility.**

### O pesadelo: ERR_CONNECTION_REFUSED

Subi o backend, subi o frontend, abri `localhost:5173`. O navegador
devolveu erro.

**Causa 1: porta errada.** O backend tava subindo na `5174` porque o
`launchSettings.json` tinha `applicationUrl: "http://localhost:5174"`.
Consertei editando pra `5000` (padrão de mercado).

**Causa 2: CORS.** Mesmo depois da porta certa, o navegador bloqueava com:

```
Access to XMLHttpRequest at 'http://localhost:5000/api/tasks'
from origin 'http://localhost:5173' has been blocked by CORS policy
```

### O que é CORS e por que existe

**CORS = Cross-Origin Resource Sharing.** É uma proteção do **navegador**
(não do servidor!). A regra:

> "Por padrão, JavaScript em `origem-A` NÃO pode fazer requests pra `origem-B`."

Origem = `protocolo + domínio + porta`. Então `localhost:5173` e
`localhost:5000` são **origens diferentes** (portas diferentes), mesmo que
seja o mesmo "site".

Pra liberar, o servidor precisa mandar:

```
Access-Control-Allow-Origin: http://localhost:5173
```

**Não é o navegador bloqueando por maldade.** É ele te protegendo de um
site malicioso em `evil.com` fazer requests pro seu banco sem você saber.

### A correção

No `Program.cs` do backend, adicionei política CORS:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevFront", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:6006")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
// ...
app.UseCors("DevFront");  // ANTES de UseHttpsRedirection
```

E tudo passou a funcionar. **Por que antes de tudo?** Ordem importa em
ASP.NET — middlewares rodam em sequência.

### A lição que ficou

> **CORS é problema de navegador, não de servidor.** O servidor aceita a
> request, mas o navegador recusa a resposta. Quando dá erro de rede no
> front, primeiro cheque **aba Network do DevTools**, não o terminal do back.

---

<a id="dia-5"></a>
## Dia 5 — Formulário, validação e o bug da data

### O que eu construí

Formulário de criar tarefa com:
- **React Hook Form** gerenciando estado
- **Zod** definindo regras de validação
- **TanStack Query Mutation** chamando a API
- onSuccess: invalida cache da lista + navega de volta

### A tríade RHF + Zod + TanStack Query

```
┌─────────────────┐
│  React Hook     │ ← estado do form (sem re-render a cada tecla)
│  Form           │
└────────┬────────┘
         │ usa
         ▼
┌─────────────────┐    ┌─────────────────┐
│  Zod schema     │ →  │  Tipos TS       │ ← inferência automática
└────────┬────────┘    └─────────────────┘
         │ valida
         ▼
┌─────────────────┐
│  useMutation    │ → POST /api/tasks
│  (TanStack)     │
└────────┬────────┘
         │ sucesso
         ▼
  invalidateQueries(['tasks'])
  navigate('/')
```

Por que essa combinação?
- **RHF** cuida do estado sem re-renderizar tudo
- **Zod** define regras tipadas (TypeScript infere o tipo do schema)
- **TanStack Mutation** lida com loading/erro/cache invalidation

### O bug da data no passado

O backend tinha a invariante "dueDate não pode ser no passado". Mas eu
deixei o campo `dueDate` no form com **default de hoje+7d**. Funcionou
na primeira vez. Aí cliquei no campo, o navegador mostrou o calendário,
eu cliquei em "hoje", e...

```
POST /api/tasks 400 Bad Request
{ error: "Prazo não pode ser no passado." }
```

### A causa raiz (depois de 3 chutes errados)

A primeira vez que chutei: "é cache do navegador". Forcei reload. Não era.
Segunda vez: "é problema do TanStack Mutation". Debug. Não era. Terceira
vez: "é o formato de data que tá errado". Não era.

Aí adicionei `console.log` no payload antes de enviar:

```typescript
[CREATE_TASK] payload enviado: {
  title: 'teste',
  description: 'outro teste',
  priority: 'Medium',
  dueDate: '2026-08-23T03:00:00.000Z'  // ← HOJE, no passado!
}
```

**O `defaultValues` do React Hook Form só roda na primeira renderização.**
Quando o usuário clica no calendário HTML `<input type="date">`, o navegador
**sobrescreve** o valor com a data que ele clicar (no caso, hoje).

### O fix

1. **Removi o default de data** — campo começa vazio, usuário escolhe
2. **Adicionei validação Zod** que rejeita passado com mensagem clara:
   ```typescript
   dueDate: z.string()
     .min(1, 'Vencimento é obrigatório.')
     .refine((val) => {
       if (!/^\d{4}-\d{2}-\d{2}$/.test(val)) return false;
       return val >= todayString();
     }, 'Vencimento não pode ser no passado.')
   ```

### A lição que ficou

> **Quando um bug persiste por mais de 2 chutes, adicione log e leia o
> payload real.** A gente "perdeu" 3 mensagens por não ter olhado antes.
> O log resolveu em 30 segundos.

---

<a id="dia-6"></a>
## Dia 6 — Componentes reutilizáveis + Storybook

### O que eu construí

Dois componentes com stories:
- **`TaskStatusChip`**: chip colorido por status (4 stories)
- **`TaskCard`**: card visual completo (6 stories)

### Por que componentes visuais separados?

ListItem do MUI é genérico. Pra mostrar tudo de uma tarefa (status, prioridade,
descrição, vencimento, comentários) fica um Frankenstein de props. Solução:
componente próprio.

```
<TaskCard task={task} onClick={handleClick} />
```

Interno:
- Borda esquerda colorida por prioridade (cinza/azul/vermelho)
- Chip de status no canto
- Descrição truncada em 120 chars
- Chips de prioridade, vencimento ("Vence em 3 dias"), contagem de comentários
- Concluída: opacity 0.7 + line-through no título
- Atrasada: ícone de aviso (WarningAmber)

### O que é Storybook e por que é útil

Storybook é um **playground de componentes**. Em vez de rodar a app inteira
pra ver um componente, você abre `localhost:6006` e vê:

- Cada componente isolado
- Controles pra variar props (status, prioridade, etc)
- Acessibilidade checada automaticamente (addon a11y)
- Visualização em diferentes estados

**Quando vale a pena:** qualquer componente reutilizável. Não vale pra
páginas compostas (que dependem de contexto).

### Por que isso é relevante pra portfólio

Recrutador abre o Storybook, vê6 stories do `TaskCard`, entende em 30
segundos o que o componente faz em cada cenário. **Comunica cuidado
com qualidade visual** sem você precisar explicar.

---

<a id="dia-7"></a>
## Dia 7 — Polimento, README honesto e o LinkedIn

### O que eu fiz

- **Compactei comentários**: tirei JSDoc verbosos, deixei só o "por quê"
- **Reescrevi os READMEs** com tom profissional e **honesto**:
  - Status do projeto dividido em ✅ Maduro / 🟡 Em progresso / 🔴 Não feito
  - Badges com versões REAIS (não React 18 fake, mas React 19 real)
  - Decisões de arquitetura explicadas em 5 bullets
- **Criei o ADR-0001** documentando por que aceitamos enums como string
- **Documente a semente de dados** (`seed-demo.py`) pra demo local

### O que eu aprendi sobre "mostrar trabalho"

Antes eu escreveria README cheio de "powered by", "production ready",
"fully featured". Agora escrevi:

> "Este é um projeto de **aprendizado em construção**. Não é um SaaS
> pronto pra produção — é um **template honesto** com peças reais."

**Por quê?** Porque recrutador sênior vê "production ready" num projeto
de 3 semanas e ri. Vê "template de aprendizado com peças reais" e pensa
"esse cara sabe a diferença entre feito e aspiracional".

### A frase-chave do dia

> **Documentação > código bonito.** Um ADR com 1 página vale mais que
> 10 helpers sem contexto.

---

<a id="apêndice"></a>
## Apêndice — Padrões que aprendi nesse caminho

### 1. **Camadas DDD** — pirâmide de dependência

```
        Api
        ↓
        Infrastructure
        ↓
        Application
        ↓
        Domain (puro, sem refs)
```

### 2. **CQRS** — separar Commands de Queries

```
Commands (mudam estado)         Queries (só leem)
        ↓                              ↓
CreateTaskHandler         GetTaskByIdHandler
ConcludeTaskHandler         ListTasksHandler
AssignTaskHandler
```

Handlers diferentes pra intenções diferentes. Mais fácil de testar, mais
fácil de raciocinar.

### 3. **Repository Pattern** — Domain não conhece o banco

```csharp
// Domain define o contrato
public interface ITaskRepository {
    Task<TaskItem?> GetByIdAsync(TaskItemId id);
    Task SaveAsync(TaskItem task);
}

// Infrastructure implementa (InMemory hoje, Cosmos amanhã)
public class InMemoryTaskRepository : ITaskRepository { ... }
```

Quando você trocar InMemory por SQL, **só Infrastructure muda**.

### 4. **Result Pattern** — erros esperados ≠ exceções

```csharp
public Result<TaskItem> Create(string title) {
    if (string.IsNullOrWhiteSpace(title))
        return Result.Fail<TaskItem>("Título vazio");
    return Result.Ok(new TaskItem(title));
}
```

### 5. **Factory Method** — construtor privado + factory que valida

```csharp
private TaskItem() { }  // EF Core usa isso pra materializar
public static Result<TaskItem> Create(...) { /* valida */ }
```

### 6. **Domain Events** — agregado fala sem saber pra quem

```csharp
AddDomainEvent(new TaskConcludedEvent(...));
// Amanhã: alguém trata. Hoje: ninguém.
```

### 7. **React + TanStack Query** — `useQuery`/`useMutation`

```typescript
const { data } = useQuery({ queryKey: ['tasks'], queryFn: getTasks });
const mutation = useMutation({
  mutationFn: createTask,
  onSuccess: () => queryClient.invalidateQueries({ queryKey: ['tasks'] }),
});
```

Cache por chave (`['tasks']`, `['task', id]`, `['tasks', filters]`).
Mutations invalidam chaves pra UI atualizar.

### 8. **Zod como contrato único** — schema gera tipo

```typescript
const taskSchema = z.object({ title: z.string().min(3) });
type Task = z.infer<typeof taskSchema>;  // TypeScript inferiu
```

### 9. **Storybook por componente**, não da app

Cada componente isolado, com controles. Designers/POs revisam sem rodar.

### 10. **ADR** — registrar decisão, não só código

Cada decisão arquitetural vira 1 arquivo `docs/adr/0001-titulo.md` com:
contexto, decisão, consequências, alternativas rejeitadas.

---

## Reflexão final

Em 3 semanas eu parti de "zero .NET" pra ter um microsserviço com:

- Domain puro com invariantes testadas
- CQRS com handlers separados
- Infrastructure trocável
- API HTTP documentada
- Frontend React completo com tema customizado
- Validação tipada end-to-end
- Documentação de decisão arquitetural
- 2 repos no GitHub com READMEs honestos

**O que ainda falta (e tá documentado no roadmap):**
- Persistência real (Cosmos DB ou SQL Azure)
- Deploy no Azure App Service
- CI/CD pipeline
- Logging estruturado
- Autenticação

Mas o **esqueleto** tá sólido. Quem abrir o código vai entender a
arquitetura. Quem ler o ADR vai entender as decisões. Quem rodar os
testes vai ver que as regras de negócio são protegidas.

**Próximo passo:** Azure deploy. Mas isso é outra história.

---

*Se você leu até aqui: obrigado. Se você quer dar feedback ou code review,
o repositório tá aberto: [github.com/0xknob/task-app](https://github.com/0xknob/task-app)
e [github.com/0xknob/task-app-web](https://github.com/0xknob/task-app-web).*