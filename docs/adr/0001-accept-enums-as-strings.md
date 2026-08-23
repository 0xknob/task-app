---
status: accepted
date: 2026-08-23
deciders: 0xknob, mentor técnico IA
---

# ADR-0001: Aceitar enums do C# como strings no JSON de entrada da API

## Contexto e problema

O backend expõe endpoints HTTP (ex.: `POST /api/tasks`) que recebem JSON
com campos enum do domínio (ex.: `Priority { Low, Medium, High }`).

Por padrão, o ASP.NET Core **serializa e deserializa enums como números** —
`{"priority": 2}` em vez de `{"priority": "High"}`. Isso causa dois
problemas concretos:

1. **Desalinhamento com o front-end TypeScript.** O front (`task-app-web`)
   já tipa o campo como `priority: 'Low' | 'Medium' | 'High'` (string).
   Aceitar só números no backend obriga converter em todo lugar — e
   qualquer divergência de ordem no enum do C# quebra o cliente em
   produção sem aviso.

2. **Legibilidade em testes manuais e `curl`.** Ver `{"priority": 2}` no
   terminal força o dev a olhar o código pra saber o que significa. Ver
   `{"priority": "High"}` é autoexplicativo.

## Decisão

Adicionar `JsonStringEnumConverter` na configuração de JSON do
`Tasks.Api/Program.cs`. Isso afeta **apenas a desserialização** (request
body). A serialização (resposta) já retornava string via `.ToString()`
no `TaskMapping.cs`, então o efeito é simétrico nos dois sentidos.

## Consequências

**Positivas:**

- Contrato HTTP autodocumentado (`"High"` em vez de `2`).
- Front e back compartilham a mesma representação de enum.
- `curl` e Swagger ficam legíveis sem consulta cruzada.
- Adicionar novo valor no enum não muda o "número mágico" dos antigos
  (com números, inserir `Critical` no meio quebraria clientes).

**Negativas / trade-offs:**

- Payload JSON fica marginalmente maior (`"High"` = 6 bytes vs `2` = 1 byte).
  Irrelevante na prática.
- Quebra clientes existentes que enviam números — mas como esse microsserviço
  é novo (sem clientes em produção ainda), o custo é zero.
- Validação automática de valores inválidos: o conversor rejeita `"Foo"`
  com 400 (bom), mas a mensagem de erro é genérica do ASP.NET, não
  customizada por domínio.

## Alternativas consideradas

- **Manter números (padrão .NET):** rejeitado pelos problemas de UX e
  acoplamento com o front.
- **Strings só no request, números na response:** rejeitado por
  assimetria — o cliente teria que tratar os dois formatos.
- **ViewModels/DTOs separados com strings:** rejeitado por adicionar
  uma camada só pra converter, sem benefício real (a `TaskMapping` já
  existe e cuida de outras conversões).

## Referências

- Commit: `2441287` — `fix(api): accept enums as strings in request body`
- `Tasks.Api/Program.cs` — registro do `JsonStringEnumConverter`
- Front: `task-app-web/src/types/task.ts` — `Priority = 'Low' | 'Medium' | 'High'`
