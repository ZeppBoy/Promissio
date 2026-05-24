# Phase 0 — Foundation (Week 1)

> **Companion:** всегда подгружать вместе с `00-core.md`.
> **Weeks:** 1
> **Goal:** Establish project skeleton, conventions, and infrastructure baseline.

---

## Tasks

1. Create solution structure with the following projects:
   - `src/Promissio.Domain` — class library, no external dependencies except NodaTime.
   - `src/Promissio.Application` — application services, MediatR handlers.
   - `src/Promissio.Infrastructure` — EF Core, Marten, external integrations.
   - `src/Promissio.Api.Origination` — origination HTTP surface.
   - `src/Promissio.Api.Servicing` — servicing HTTP surface.
   - `src/Promissio.BatchProcessor` — daily batch worker service.
   - `src/Promissio.AI` — AI orchestration and agents.
   - `src/Promissio.AI.McpServer` — standalone MCP server.
   - `tests/Promissio.Domain.Tests`
   - `tests/Promissio.Application.Tests`
   - `tests/Promissio.Integration.Tests`
   - `tests/Promissio.AI.Evals`
2. Configure `Directory.Build.props` and `Directory.Packages.props` for Central Package Management.
3. Set up `.editorconfig`, `.gitignore`, `.gitattributes`.
4. Add `docker-compose.yml` with PostgreSQL 16, Qdrant, Langfuse, Jaeger.
5. Write initial `README.md` skeleton including project description, goals, status, and getting-started instructions.
6. Write `AGENTS.md` with coding standards, naming conventions, and AI-agent operating instructions.
7. Write `CLAUDE.md` with Claude Code-specific guidance.
8. Configure GitHub Actions: build and test on every pull request.
9. Add `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `LICENSE` (MIT or Apache 2.0).

## Acceptance criteria

- `dotnet build` succeeds from a clean clone.
- `dotnet test` runs and passes (initially zero tests, but framework operational).
- `docker compose up` brings up all infrastructure services.
- README has working "Getting started" section reproducible by a stranger.

## AI delegation notes

Phase 0 should be done with minimal AI assistance. The author must internalize the foundation before delegating subsequent work. AI tools can help with `.editorconfig`, `.gitignore`, GitHub Actions YAML — but the project structure itself is an architectural decision to own personally.
