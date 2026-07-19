# SmartGrader

Full-stack educational grading system (monorepo).

- `server/` — ASP.NET Core Web API (.NET 8), Clean Architecture + CQRS
- `client/` — Angular 17 (standalone components), PrimeNG UI

> Detailed rules per area (auto-loaded when working in that directory):
>
> - Backend rules → [server/CLAUDE.md](server/CLAUDE.md)
> - Frontend rules → [client/CLAUDE.md](client/CLAUDE.md)

## Repository Structure

```
root/
├── server/          ← C# backend (SmartGrader.sln lives here)
│   ├── Api/         ← Controllers, Middleware, BackgroundServices
│   ├── Application/ ← Use cases (CQRS), DTOs, Services, Validators
│   ├── Domain/      ← Entities, Abstractions (interfaces), no dependencies
│   └── Infrastructure/ ← EF Core, Repositories, External services (OpenAI, SMTP)
└── client/          ← Angular frontend
    └── src/app/
        ├── core/    ← ApiClient, interceptors, guards, validators
        ├── models/  ← TypeScript interfaces (DTOs)
        ├── pages/   ← Feature components (lessons, students, assignments, submissions, logs)
        └── services/← One service per entity
```

## Other project docs (originally written for GitHub Copilot, still valid reference)

- `.github/prompts/*.prompt.md` — feature planning docs (one per feature, e.g. Excel import/export, Hebrew dates). Historical design record, not reusable templates.
- `.github/agents/*.agent.md` — Copilot custom agent personas used to build specific features. Reference for *how* a feature was approached, not something this tool invokes directly.
- `.github/skills/*/SKILL.md` — reusable backend/frontend/UX pattern docs (naming conventions, real code examples, pitfalls). Mirrored into [.claude/skills/](.claude/skills/) so they load automatically here too — keep both copies in sync when a pattern changes.
- `.github/תיקונים.md` — running Hebrew bug/TODO list.
