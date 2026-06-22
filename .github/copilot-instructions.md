# SmartGrader – Copilot Instructions

This is a full-stack educational grading system (monorepo).

- `server/` — ASP.NET Core Web API (.NET 8), Clean Architecture + CQRS
- `client/` — Angular 17 (standalone components), PrimeNG UI

> Detailed rules per area:
>
> - Backend rules → `.github/instructions/server.instructions.md` (applies to `server/**`)
> - Frontend rules → `.github/instructions/client.instructions.md` (applies to `client/**`)

---

## Repository Structure

```
root/
├── server/          ← C# backend (SmartGrader.sln lives here)
│   ├── Api/         ← Controllers, Middleware, BackgroundServices
│   ├── Application/ ← Use cases (CQRS), DTOs, Services, Validators
│   ├── Domain/      ← Entities, Abstractions (interfaces), no dependencies
│   └── Infrastructure/ ← EF Core, Repositories, External services (OpenAI)
└── client/          ← Angular frontend
    └── src/app/
        ├── core/    ← ApiClient, interceptors
        ├── models/  ← TypeScript interfaces (DTOs)
        ├── pages/   ← Feature components (lessons, students, assignments, submissions)
        └── services/← One service per entity
```
