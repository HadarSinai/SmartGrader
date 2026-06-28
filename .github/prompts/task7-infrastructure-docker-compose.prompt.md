---
description: "Task 7 — Infrastructure: Set up Judge0 with Docker Compose for sandboxed code execution on the production server"
agent: agent
tools: [read_file, create_file, run_in_terminal]
---

# Task 7 — Infrastructure: Judge0 Docker Compose Setup

**This task runs on the production server, not in the VS Code workspace.**

**Depends on:** Task 3 (Judge0CodeRunner must be implemented and configured)

---

## Context

Judge0 is an open-source code execution system. It runs student code inside isolated Docker containers with:

- CPU and memory limits per submission
- No network access inside the container
- No access to the host filesystem
- Configurable time limit (default 10 seconds)

Official repo: https://github.com/judge0/judge0

---

## Step 1 — Prerequisites on the server

Ensure the following are installed:

```bash
docker --version       # Docker 20+ required
docker compose version # Docker Compose v2 required
```

If not installed (Ubuntu/Debian):

```bash
curl -fsSL https://get.docker.com | sh
```

---

## Step 2 — Create `docker-compose.yml` at project root

Create `docker-compose.yml` at the root of the repository (same level as `server/` and `client/`):

```yaml
version: "3.9"

services:
  judge0-server:
    image: judge0/judge0:latest
    container_name: judge0
    ports:
      - "2358:2358"
    environment:
      REDIS_URL: redis://redis:6379
      DATABASE_URL: postgres://judge0:judge0@postgres:5432/judge0
      WORKERS_NUM: 2
      MAX_CPU_TIME_LIMIT: 10
      MAX_MEMORY_LIMIT: 262144 # 256MB in KB
    depends_on:
      - redis
      - postgres
    privileged: true # required for Docker-in-Docker sandbox
    restart: unless-stopped

  judge0-workers:
    image: judge0/judge0:latest
    command: ["./scripts/workers"]
    environment:
      REDIS_URL: redis://redis:6379
      DATABASE_URL: postgres://judge0:judge0@postgres:5432/judge0
    depends_on:
      - redis
      - postgres
    privileged: true
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    restart: unless-stopped

  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: judge0
      POSTGRES_PASSWORD: judge0
      POSTGRES_DB: judge0
    volumes:
      - judge0_postgres_data:/var/lib/postgresql/data
    restart: unless-stopped

volumes:
  judge0_postgres_data:
```

---

## Step 3 — Start Judge0

```bash
docker compose up -d
```

Wait ~30 seconds for Judge0 to initialize, then verify:

```bash
curl http://localhost:2358/system_info
```

Expected response: JSON with Judge0 version info.

---

## Step 4 — Verify C# works (language_id = 51)

```bash
curl -X POST http://localhost:2358/submissions?wait=true \
  -H "Content-Type: application/json" \
  -d '{
    "source_code": "using System; class Program { static void Main() { Console.WriteLine(\"hello\"); } }",
    "language_id": 51,
    "stdin": ""
  }'
```

Expected: `"stdout": "hello\n"`, `"status": {"id": 3, "description": "Accepted"}`

---

## Step 5 — Update appsettings.json for production

File: `server/Api/appsettings.json`

The `Judge0.BaseUrl` should point to Judge0 on the same server:

```json
"Judge0": {
  "BaseUrl": "http://judge0:2358",
  "TimeoutSeconds": 10,
  "LanguageId": 51
}
```

> Use `http://judge0:2358` if the API runs inside Docker Compose network.
> Use `http://localhost:2358` if the API runs directly on the host.

---

## Step 6 — Production security checklist

- [ ] Judge0 port 2358 is **NOT exposed** to the internet (firewall rule: only localhost access)
- [ ] `privileged: true` is required for Judge0 sandboxing — do not remove it
- [ ] Judge0 postgres password changed from default `judge0` in production
- [ ] API key optionally set in Judge0 config and `appsettings.json Judge0.ApiKey`

---

## Validation

- `docker compose ps` → all 4 services running
- `curl http://localhost:2358/system_info` → returns JSON
- C# test submission (Step 4) returns `status.id = 3` (Accepted)
- SmartGrader API starts and connects to Judge0 without errors
