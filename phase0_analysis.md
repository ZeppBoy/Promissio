# Phase 0 — Implementation Analysis Report

> **Audience:** Project lead / reviewers.
> **Generated:** 2026-05-22
> **Source plan:** [`developers_plan.md`](./developers_plan.md), Section 7 → "Phase 0 — Foundation (Week 1)".
> **Method:** Read the actual repository state and compared each Phase 0 task and acceptance criterion against what is on disk. Built and ran tests to verify behavior.

---

## 1. Executive Summary

Phase 0 is **largely scaffolded but not finished to its stated acceptance bar**. The skeleton, infra compose file, CI workflow, top-level docs, and ten of the eleven required projects exist. However, several elements would block a clean Phase 0 sign-off:

| Area | Status | Severity |
|---|---|---|
| Solution structure | Partial | Medium |
| Directory.Build.props / Packages.props | Done with caveats | Low |
| .editorconfig / .gitignore / .gitattributes | Done with bugs | Medium |
| docker-compose.yml | Done | Low |
| README.md | Done (functional) | Low |
| AGENTS.md / CLAUDE.md | Done | Low |
| GitHub Actions CI | Done but **would fail today** | **High** |
| CONTRIBUTING / CoC / LICENSE | Done | Low |
| Empty test projects | Present without test code | Low (allowed by AC) |
| Layering violations | Application → Infrastructure | **High** |
| Format violations | 10+ files unformatted | **High** (CI fail) |

**Bottom line:** the foundation can compile (`dotnet build` succeeds) and tests can run (`dotnet test` passes), but the CI pipeline as configured would fail on the `dotnet format --verify-no-changes` step. There is also a structural layering bug (Application references Infrastructure) and a misconfigured project (`Promissio.AI.Evals`) that is missing from the solution and has broken relative project-reference paths. These must be fixed before Phase 1 begins.

---

## 2. Task-by-Task Verification

### Task 1 — Solution structure (eleven projects)

The plan specifies eleven projects across `src/` and `tests/`. The repository contains the following:

| Required project | Present? | Notes |
|---|---|---|
| [src/Promissio.Domain](src/Promissio.Domain/Promissio.Domain.csproj) | ✅ | Only `NodaTime` referenced — satisfies "no external dependencies except NodaTime". |
| [src/Promissio.Application](src/Promissio.Application/Promissio.Application.csproj) | ✅ | **References `Promissio.Infrastructure` (layering violation, see §4).** |
| [src/Promissio.Infrastructure](src/Promissio.Infrastructure/Promissio.Infrastructure.csproj) | ✅ | References Marten. |
| [src/Promissio.Api.Origination](src/Promissio.Api.Origination/Promissio.Api.Origination.csproj) | ✅ | Still contains the `WeatherForecast` scaffold ([Program.cs:17-41](src/Promissio.Api.Origination/Program.cs#L17-L41)) — uses `DateTime.Now`, which is **banned** by `AGENTS.md` / `CLAUDE.md` rules for domain code. Outside the domain layer it is tolerated, but as starter content it should be removed before public visibility. |
| [src/Promissio.Api.Servicing](src/Promissio.Api.Servicing/Program.cs) | ✅ | Same `WeatherForecast` boilerplate as above. |
| [src/Promissio.BatchProcessor](src/Promissio.BatchProcessor/Promissio.BatchProcessor.csproj) | ✅ | Plan calls for a "daily batch worker service". Current csproj uses `Microsoft.NET.Sdk` (class library), **not** `Microsoft.NET.Sdk.Worker` — there is no Host or worker bootstrap yet. |
| [src/Promissio.AI](src/Promissio.AI/Promissio.AI.csproj) | ✅ | Generated as `Microsoft.NET.Sdk.Web` and exposes `AddOpenApi`. The plan describes this as "AI orchestration and agents", so a non-web SDK or `Worker` template would be more honest. |
| [src/Promissio.AI.McpServer](src/Promissio.AI.McpServer/Promissio.AI.McpServer.csproj) | ✅ | Web SDK; carries `ModelContextProtocol` package reference but no MCP transport wiring yet (acceptable for Phase 0). |
| [tests/Promissio.Domain.Tests](tests/Promissio.Domain.Tests/Promissio.Domain.Tests.csproj) | ⚠️ | csproj exists, **no `.cs` files** inside. Acceptable per AC ("initially zero tests"), but discovery prints a warning when running. |
| [tests/Promissio.Application.Tests](tests/Promissio.Application.Tests/Promissio.Application.Tests.csproj) | ⚠️ | Same situation — empty test project. |
| [tests/Promissio.Integration.Tests](tests/Promissio.Integration.Tests/Promissio.Integration.Tests.csproj) | ⚠️ | Empty — references Domain, Application, Infrastructure. |
| [tests/Promissio.AI.Evals](tests/Promissio.AI.Evals/Promissio.AI.Evals.csproj) | ❌ | **Not included in [Promissio.slnx](Promissio.slnx)** and has **wrong project-reference paths**: `..\Promissio.Domain\` and `..\Promissio.Application\` resolve to sibling test directories that do not contain those csprojs. Would not build if added to the solution. |

Extra projects that are not in the Phase 0 plan but exist in the repo:

- [tests/Promissio.BatchProcessor.Tests](tests/Promissio.BatchProcessor.Tests/Promissio.BatchProcessor.Tests.csproj) — contains [BatchProcessorTests.cs](tests/Promissio.BatchProcessor.Tests/BatchProcessorTests.cs).
- [tests/Promissio.Infrastructure.Tests](tests/Promissio.Infrastructure.Tests/Promissio.Infrastructure.Tests.csproj) — contains [InfrastructureTests.cs](tests/Promissio.Infrastructure.Tests/InfrastructureTests.cs).

Both contain trivial smoke tests; harmless, but should be noted (the plan deliberately consolidated Infrastructure coverage into `Promissio.Integration.Tests`).

There is also a **stray empty nested folder tree**: [src/Promissio.Domain/src/Promissio.Application/src/](src/Promissio.Domain/src/Promissio.Application/src/) — looks like an accidental scaffolding artifact. Should be deleted.

**Status: Partial.**

---

### Task 2 — `Directory.Build.props` and `Directory.Packages.props`

- [Directory.Build.props](Directory.Build.props) sets `TargetFramework=net10.0`, `ImplicitUsings=enable`, `Nullable=enable`, `LangVersion=13`. Consistent across all projects.
- [Directory.Packages.props](Directory.Packages.props) enables Central Package Management (`ManagePackageVersionsCentrally=true`) and lists 17 packages. ✅
- **Mismatch with the plan and the README.** The plan specifies `.NET 9 (current), with planned migration to .NET 10 upon GA release in November 2026.` The codebase already targets `net10.0`. The README still advertises ".NET 9" and uses a `.NET 9.0` badge ([README.md:5](README.md#L5)). Pick one and align.
- Minor: indentation inside `Directory.Packages.props` is inconsistent; not blocking.

**Status: Done with caveats — version inconsistency between plan, README, and props files.**

---

### Task 3 — `.editorconfig`, `.gitignore`, `.gitattributes`

- [.editorconfig](.editorconfig) **contains typos that silently disable several rules**:
  - `csharp_style_inlined_declARATION` (mixed case — not a real key).
  - `csharp_style_pattern_limited_objects` (not a recognized key).
  - `csharp_style_deconstructed_variableDeclaration` (mixed case).
  - `csharp_prefer_out_variableDeclaration` (mixed case).
  - `dotnet_generate_csp_project_file` at the bottom — not a real `.editorconfig` key.
  - The `[csharp]` section header should be `[*.cs]` or follow a real file glob.

  These typos do not break the build, but they mean the file is **not enforcing what it appears to enforce**. Worth a 10-minute cleanup pass.
- [.gitignore](.gitignore) ✅ — covers `bin/`, `obj/`, IDE files, OS junk.
- [.gitattributes](.gitattributes) ✅ — declares diff drivers and text normalization.

**Status: Done with bugs (editorconfig keys).**

---

### Task 4 — `docker-compose.yml`

[docker-compose.yml](docker-compose.yml) brings up:

| Service | Plan-required | Present | Notes |
|---|---|---|---|
| PostgreSQL 16 | ✅ | ✅ | `postgres:16-alpine`, healthcheck included. |
| Qdrant | ✅ (option) | ✅ | `qdrant:v1.12.2`, persistent volume. |
| Langfuse | ✅ | ✅ | Wired to its own `langfuse-postgres`. |
| Jaeger | ✅ | ✅ | OTLP on 4317/4318, UI on 16686. |

Smaller observations:

- Top-level `version: "3.9"` is **obsolete in Compose v2** and will print a warning. Safe to remove.
- Langfuse uses `:latest` — pin a version for reproducibility.
- Langfuse's `NEXTAUTH_URL`, `SALT`, and ClickHouse dependencies are missing — the official Langfuse v3 image will not start cleanly with this config. This means the acceptance criterion "`docker compose up` brings up all infrastructure services" is probably **not actually satisfied** for Langfuse — needs hands-on verification.

**Status: Done at the structural level; Langfuse service likely broken on `up`.**

---

### Task 5 — Initial README.md

[README.md](README.md) is substantial (~16 KB, 300+ lines), with mission, architecture diagram, phase table, tech stack, "Getting started", project structure, and documentation index. ✅

Caveats:

- Refers to directories that do not yet exist: `docs/`, `benchmarks/`, `seed-data/`, `docs/architecture/`, `docs/mcp/setup.md`. Per `CLAUDE.md` §8 Pitfall 6 ("Generating documentation that overstates completeness"), these should either be created as placeholders or marked clearly as "Phase N planned".
- References .NET 9 (see §3 above) while code targets `net10.0`.
- "Getting started" instructions are *almost* runnable from a clean clone — `dotnet build` and `dotnet test` work, but `docker compose up` may not fully succeed due to the Langfuse config gap.

**Status: Done (functional first draft); accuracy needs trim.**

---

### Task 6 — `AGENTS.md`

[AGENTS.md](AGENTS.md) is present (~25 KB). Not re-reviewed in detail here, but its existence and reference from `CLAUDE.md` and `CONTRIBUTING.md` satisfy the Phase 0 deliverable. ✅

---

### Task 7 — `CLAUDE.md`

[CLAUDE.md](CLAUDE.md) is present and references `AGENTS.md` as authoritative. ✅

---

### Task 8 — GitHub Actions CI

[.github/workflows/ci.yml](.github/workflows/ci.yml) builds and tests on push to `main` and on PRs. ✅

Issues:

- Uses `dotnet-version: "10.0.x"` — consistent with the actual target, but inconsistent with the README's ".NET 9" claim.
- Includes a `dotnet format --verify-no-changes` step which **fails right now** on the current main branch (see §4 below). This means the very first PR would be blocked on formatting unless the file is touched up first.
- No caching of NuGet packages — slow but acceptable for Phase 0.
- No matrix (only `ubuntu-latest`). Fine for Phase 0.

**Status: Done structurally, but the workflow would fail on its own repository today.**

---

### Task 9 — `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `LICENSE`

- [CONTRIBUTING.md](CONTRIBUTING.md) ✅ — Conventional Commits, formatting expectations, PR size limit.
- [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) ✅ — Contributor Covenant 2.1 adaptation.
- [LICENSE](LICENSE) ✅ — MIT, as one of the options offered by the plan.

**Status: Done.**

---

## 3. Acceptance-Criteria Check

| Criterion | Verified outcome | Pass? |
|---|---|---|
| `dotnet build` succeeds from a clean clone. | Build succeeded with 0 warnings / 0 errors. | ✅ |
| `dotnet test` runs and passes (initially zero tests). | Runs; two extra smoke tests pass; three required test projects report "no tests available" warnings but the run does not fail. | ✅ (with caveat) |
| `docker compose up` brings up all infrastructure services. | Not actually verified end-to-end. Langfuse service is under-configured and is likely to fail to start. PostgreSQL, Qdrant, Jaeger should be fine. | ⚠️ Likely partial |
| README has working "Getting started" section reproducible by a stranger. | The commands are correct and self-contained; the section references docs/setup paths that do not yet exist, but the core flow (clone → compose → build → test) is reproducible. | ✅ (with caveat) |

---

## 4. Cross-Cutting Issues

### 4.1 Layering violation — Application references Infrastructure

[src/Promissio.Application/Promissio.Application.csproj](src/Promissio.Application/Promissio.Application.csproj#L5) has:

```xml
<ProjectReference Include="..\Promissio.Infrastructure\Promissio.Infrastructure.csproj" />
```

This inverts the standard Clean/Onion dependency direction described by the architecture diagram in `developers_plan.md` §5 ("Application Layer → Domain Core → Infrastructure"). It is also at odds with the project description in the plan: `Promissio.Application` is "application services, MediatR handlers"; `Promissio.Infrastructure` is "EF Core, Marten, external integrations". Infrastructure should depend on Application/Domain, not the other way around.

**Recommendation:** Remove this reference. Wire Marten/EF dependencies through the API composition root (`Promissio.Api.*` projects) or through DI abstractions defined in Application.

**Severity:** High — this is the foundation; if left, every later phase will inherit the violation.

### 4.2 `dotnet format --verify-no-changes` fails

Running locally produced ~10 formatting errors across:

- `src/Promissio.Application/Validators.cs` (whitespace + missing final newline).
- `src/Promissio.AI.McpServer/Program.cs`, `src/Promissio.AI/Program.cs`, `src/Promissio.BatchProcessor/BatchProcessorService.cs`, `src/Promissio.Domain/DomainService.cs`, `src/Promissio.Infrastructure/InfrastructureService.cs`, `tests/Promissio.BatchProcessor.Tests/BatchProcessorTests.cs`, `tests/Promissio.Infrastructure.Tests/InfrastructureTests.cs`, `src/Promissio.Application/ApplicationService.cs` (missing final newline).

This means **the CI workflow as committed would fail on the very first PR** until a `dotnet format` pass is run and committed.

**Severity:** High — easy to fix, but blocks the workflow.

### 4.3 Misconfigured `Promissio.AI.Evals` project

[tests/Promissio.AI.Evals/Promissio.AI.Evals.csproj](tests/Promissio.AI.Evals/Promissio.AI.Evals.csproj#L18-L21):

```xml
<ProjectReference Include="..\Promissio.Domain\Promissio.Domain.csproj" />
<ProjectReference Include="..\Promissio.Application\Promissio.Application.csproj" />
```

These relative paths point to sibling directories under `tests/`, which do not contain the referenced csprojs. They should be `..\..\src\Promissio.Domain\...` and `..\..\src\Promissio.Application\...` (matching the convention used by every other test project).

Additionally, the project is **absent from [Promissio.slnx](Promissio.slnx)** — `dotnet build` from the repo root never compiles it, which is why the broken references have not yet been noticed.

**Severity:** Medium — does not block Phase 0 today, but will explode the moment AI evaluations start (Phase 7+).

### 4.4 `decimal` in Application-layer DTO

[src/Promissio.Application/Validators.cs:19](src/Promissio.Application/Validators.cs#L19) declares:

```csharp
public record LoanApplication(decimal Amount, int TermInMonths);
```

`AGENTS.md` and `CLAUDE.md` (§8 Pitfall 2) ban `decimal` as a money primitive in domain APIs. While `LoanApplication` here is in the Application layer (not Domain), this scaffold sets a bad precedent before `Money` even exists. Either (a) delete this placeholder until Phase 1's `Money` value object lands, or (b) explicitly comment it as a placeholder to be replaced.

**Severity:** Low — but worth fixing alongside the Phase 1 value object work so the example never enters the rest of the codebase by copy-paste.

### 4.5 `System.DateTime` usage in API scaffolds

[src/Promissio.Api.Origination/Program.cs:27](src/Promissio.Api.Origination/Program.cs#L27) and [src/Promissio.Api.Servicing/Program.cs:27](src/Promissio.Api.Servicing/Program.cs#L27) both retain the default `WeatherForecast` template, which uses `DateTime.Now`. These are template artifacts and outside domain code, so they do not technically violate the NodaTime rule, but they should be removed before the first real endpoint is added (Phase 4) so they don't become reference patterns.

**Severity:** Low — pure scaffolding.

### 4.6 Empty test projects produce noisy `dotnet test` output

`Promissio.Domain.Tests`, `Promissio.Application.Tests`, and `Promissio.Integration.Tests` all build but contain no `.cs` files, which makes `dotnet test` print:

```
No test is available in ...Promissio.Domain.Tests.dll. Make sure that test discoverer & executors are registered ...
```

The Phase 0 acceptance criterion allows zero tests, so this is *not* a failure. But adding a single placeholder `[Fact] public void Placeholder() { }` per project would silence the warning and avoid masking real configuration drift later.

**Severity:** Cosmetic.

### 4.7 `developers_plan.md` "Last updated" date is in the future

The plan header says `Last updated: 2026-05-17`. The local date is **2026-05-22**. This is consistent (plan was updated 5 days before today). No action needed; flagged here only to confirm dates are not stale.

---

## 5. Recommendations Before Closing Phase 0

In priority order:

1. **Fix the Application → Infrastructure project reference** (§4.1). Run `dotnet build` to confirm Application still compiles without it.
2. **Run `dotnet format` and commit the result** (§4.2) so CI passes.
3. **Fix `Promissio.AI.Evals` project references and add it to `Promissio.slnx`** (§4.3).
4. **Reconcile the .NET version story across `Directory.Build.props`, README badge, CI workflow, and `developers_plan.md`.** Either commit to `net10.0` everywhere or revert to `net9.0` until GA.
5. **Either remove or stub the Langfuse service in `docker-compose.yml`** so `docker compose up` truly succeeds.
6. **Clean up the typos in `.editorconfig`** so the formatter actually enforces what the file claims to enforce.
7. **Delete the stray `src/Promissio.Domain/src/Promissio.Application/src/` directory tree.**
8. **Remove the `WeatherForecast` scaffold** from `Promissio.Api.Origination` and `Promissio.Api.Servicing`.
9. **Decide whether `Promissio.BatchProcessor` should be `Microsoft.NET.Sdk.Worker`** (true worker template) before Phase 5 starts.
10. **Document the docs/, benchmarks/, seed-data/ directories** — either create empty `.gitkeep` placeholders or remove their mentions from the README until they exist.

None of these are large changes. Together they would bring Phase 0 from "scaffolded" to genuinely meeting its acceptance criteria, and they protect every subsequent phase from inheriting structural drift.

---

## 6. What Was Done Well

- The breadth of scaffolding is impressive for one week of part-time work: 8 source projects, 5 test projects, working CI, working docker-compose for the non-AI infra, and substantial top-level docs (`README`, `AGENTS.md`, `CLAUDE.md`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `LICENSE`).
- Central Package Management is enabled from day one, which avoids the common "version drift across csprojs" problem.
- `Promissio.Domain` correctly has only `NodaTime` as an external dependency — the most important rule of the project is encoded structurally.
- `AGENTS.md` and `CLAUDE.md` are written, referenced, and consistent — the AI-pair-programming part of the workflow is set up correctly.
- Conventional Commits and a code-of-conduct are in place before any external contributors arrive.

---

*End of Phase 0 analysis. This report reflects the repository state at 2026-05-22 on branch `main`.*
