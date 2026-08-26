#!/usr/bin/env python3
"""
Seed script: cria 8 tarefas realistas pra demo do portfólio.
Contexto: dev junior montando portfolio fullstack.

Uso: python seed-demo.py
Requer: backend rodando em http://localhost:5000
"""

import json
import sys
import urllib.request
from urllib.error import HTTPError, URLError

API = "http://localhost:5000/api/tasks"
ASSIGNEE_A = "11111111-1111-1111-1111-111111111111"
ASSIGNEE_B = "22222222-2222-2222-2222-222222222222"


def post(path: str, payload: dict) -> tuple[int, dict | None]:
    """POST e retorna (status_code, body_dict or None)."""
    url = f"{API}{path}"
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(
        url,
        data=data,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req) as resp:
            body_bytes = resp.read()
            body = json.loads(body_bytes.decode("utf-8")) if body_bytes else None
            return resp.status, body
    except HTTPError as e:
        body_bytes = e.read()
        body = json.loads(body_bytes.decode("utf-8")) if body_bytes else None
        return e.code, body


def create_task(title: str, description: str, priority: str, due_date: str) -> str | None:
    status, body = post("", {
        "title": title,
        "description": description,
        "priority": priority,
        "dueDate": due_date,
    })
    if status != 201:
        print(f"  [FALHA] '{title}' -> status {status}: {body}", file=sys.stderr)
        return None
    return body["id"]


def assign_task(task_id: str, user_id: str) -> None:
    post(f"/{task_id}/assign", {"assigneeUserId": user_id})


def conclude_task(task_id: str) -> None:
    post(f"/{task_id}/conclude", {})


def add_comment(task_id: str, author: str, content: str) -> None:
    post(f"/{task_id}/comments", {
        "authorUserId": author,
        "content": content,
    })


def list_tasks() -> list[dict]:
    """Pega todas as tarefas pra listar no final."""
    with urllib.request.urlopen(API) as resp:
        return json.loads(resp.read().decode("utf-8"))


def main() -> None:
    print("Criando 8 tarefas de demo...\n")

    # 1. Concluida - Baixa
    t1 = create_task(
        "Configurar ambiente de desenvolvimento",
        "Instalar .NET 10 SDK, Node 24, VS Code e Git. Configurar .gitignore pra ignorar bin/, obj/, node_modules/, dist/.",
        "Low", "2026-08-27T00:00:00Z",
    )
    if t1:
        assign_task(t1, ASSIGNEE_A)
        conclude_task(t1)
        add_comment(t1, ASSIGNEE_A, "Ambiente pronto, todos os testes rodando.")

    # 2. Concluida - Media
    t2 = create_task(
        "Subir primeiro commit com solution DDD",
        "Criar 4 projetos: Domain (puro), Application, Infrastructure, Api. Sem dependencias externas no Domain.",
        "Medium", "2026-08-28T00:00:00Z",
    )
    if t2:
        assign_task(t2, ASSIGNEE_A)
        conclude_task(t2)

    # 3. Em progresso - Alta
    t3 = create_task(
        "Implementar TaskCard com Material Design M2",
        "Componente reutilizavel com borda colorida por prioridade, status chip, e chip de vencimento. 6 stories no Storybook cobrindo todos os estados.",
        "High", "2026-08-29T00:00:00Z",
    )
    if t3:
        assign_task(t3, ASSIGNEE_A)
        add_comment(t3, ASSIGNEE_A, "Ja criei o componente e as stories. Falta validar com o time.")
        add_comment(t3, ASSIGNEE_B, "Curti o design da borda colorida. Sugestao: usar elevation 2 no hover.")

    # 4. Pendente - Alta
    t4 = create_task(
        "Escrever ADR-0002 sobre CQRS",
        "Documentar decisao de separar Commands e Queries. Contexto: complexidade do agregado vs simplicidade dos handlers.",
        "High", "2026-08-27T00:00:00Z",
    )
    if t4:
        assign_task(t4, ASSIGNEE_A)

    # 5. Pendente - Media
    t5 = create_task(
        "Configurar pipeline de CI no GitHub Actions",
        "Rodar dotnet test + npm run build em todo PR. Bloquear merge se algum falhar.",
        "Medium", "2026-08-28T00:00:00Z",
    )

    # 6. Pendente - Alta
    t6 = create_task(
        "Deploy da API no Azure App Service",
        "Criar App Service plano Free. Configurar connection string pra Cosmos DB. Deploy via GitHub Actions.",
        "High", "2026-08-29T00:00:00Z",
    )
    if t6:
        assign_task(t6, ASSIGNEE_A)

    # 7. Pendente - Baixa
    create_task(
        "Estudar MCP (Model Context Protocol)",
        "Ler a spec oficial. Entender diferenca entre MCP servers e ferramentas nativas de agente.",
        "Low", "2026-09-10T00:00:00Z",
    )

    # 8. Em progresso - Media (com varios comentarios)
    t8 = create_task(
        "Documentar decisoes de UX no Storybook",
        "Criar MDX explicando por que cada decisao visual foi tomada. Designers precisam revisar.",
        "Medium", "2026-08-30T00:00:00Z",
    )
    if t8:
        assign_task(t8, ASSIGNEE_A)
        add_comment(t8, ASSIGNEE_A, "Comecei pela documentacao do TaskStatusChip.")
        add_comment(t8, ASSIGNEE_B, "Boa. Nao esquece de incluir o rationale das cores.")
        add_comment(t8, ASSIGNEE_A, "Boa pegada, vou adicionar.")

    # Listagem final
    print("\n" + "=" * 70)
    tasks = list_tasks()
    print(f"Total: {len(tasks)} tarefas no InMemory\n")

    status_icon = {"Pending": "[PENDENTE]", "InProgress": "[EM PROGRESSO]", "Concluded": "[CONCLUIDA]"}
    prio_label = {"High": "ALTA", "Medium": "MEDIA", "Low": "BAIXA"}

    for t in tasks:
        due = t["dueDate"][:10]
        st = status_icon.get(t["status"], t["status"])
        pri = prio_label.get(t["priority"], t["priority"])
        n_comments = len(t["comments"])
        assignee_short = t["assignee"]["userId"][:8] if t["assignee"] else "sem assignee"
        print(f"  {st:14} | {pri:5} | {t['title'][:45]:45} | vence: {due} | {assignee_short} | {n_comments} comentario(s)")

    print("\nPronto pro print do LinkedIn!")


if __name__ == "__main__":
    main()