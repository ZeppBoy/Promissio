# Promissio — Plan, разбитый для агента

> **Назначение:** позволить агенту с ограниченным контекстом (Qwen3 27B, 80k) работать с планом проекта, подгружая только релевантные куски.
> **Источник:** `developers_plan.md` (исходный монолитный документ).
> **Правило:** при разногласии между этими файлами и исходным `developers_plan.md` — исходник имеет приоритет до момента, пока чанки не будут вручную синхронизированы.

---

## Как агент должен это читать

### 1. Всегда подгружать

| Файл | Что внутри | Размер (≈ токены) |
|---|---|---|
| `00-core.md` | Миссия, цели, non-goals, стек, архитектура, доменная модель, quality standards, правила работы с AI-агентами, глоссарий | ~3.5k |

### 2. Подгружать под текущую задачу (одну фазу за раз)

| Файл | Фаза | Недели | Тема |
|---|---|---|---|
| `01-phase-00-foundation.md` | 0 | 1 | Скелет решения, тулинг, инфраструктура |
| `02-phase-01-interest-engine.md` | 1 | 2–4 | Value objects, day-count conventions, interest calculator |
| `03-phase-02-schedules-aprc.md` | 2 | 5–6 | Annuity/Diff/Bullet/Custom schedules, APRC |
| `04-phase-03-loan-aggregate.md` | 3 | 7–8 | Loan aggregate, state machine, Marten event sourcing |
| `05-phase-04-apis.md` | 4 | 9–10 | Origination API, Servicing API |
| `06-phase-05-batch-processor.md` | 5 | 11–12 | Daily batch, IFRS 9 staging, provisioning |
| `07-phase-06-mcp-server.md` | 6 | 13–15 | MCP server, banker tools |
| `08-phase-07-ai-agents.md` | 7 | 16–18 | Credit decisioning, early warning, collections agents |
| `09-phase-08-evals-observability.md` | 8 | 19–20 | Evaluation framework, OTel + Langfuse |
| `10-phase-09-production-polish.md` | 9 | 21–22 | Performance, reliability, документация |
| `11-phase-10-public-launch.md` | 10 | 23 | Публикация, доклады, аутрич |

### 3. Подгружать редко

| Файл | Когда нужен |
|---|---|
| `12-launch-strategy-and-roadmap.md` | Когда речь о build-in-public, выборе площадок для публикации, или о Year 2 / Year 3 roadmap |

---

## Типовые рецепты загрузки

**Работа над конкретной задачей (например, Phase 1 Week 3 — day-count conventions):**
```
00-core.md + 02-phase-01-interest-engine.md
≈ 4.5k токенов
```

**Подготовка к code review для PR в инфраструктуру batch-процессора:**
```
00-core.md + 06-phase-05-batch-processor.md
≈ 5k токенов
```

**Разговор о доменной модели / архитектуре без привязки к фазе:**
```
00-core.md only
≈ 3.5k токенов
```

**Планирование запуска и публикации:**
```
00-core.md + 11-phase-10-public-launch.md + 12-launch-strategy-and-roadmap.md
≈ 6k токенов
```

---

## Что НЕ попало в чанки

- ADR (`/docs/adr/`) — храните отдельно, агент подгружает только тот ADR, который относится к текущей задаче.
- Domain docs (`/docs/domain/`) — формулы day-count conventions, APRC, IFRS 9 staging. Агент подгружает по необходимости.
- `AGENTS.md`, `CLAUDE.md` — операционные инструкции, должны быть отдельной всегда-загружаемой парой.

## Поддержка

- При изменении исходного `developers_plan.md` обновляйте соответствующий чанк и **README.md** (этот файл).
- Если фаза разрастается — разбейте её ещё на per-week файлы (`02a-phase-01-week-02.md`, `02b-phase-01-week-03.md` и т.д.). Сейчас фазы достаточно компактные, чтобы держать их одним файлом.
