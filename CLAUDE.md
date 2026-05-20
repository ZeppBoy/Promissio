# Promissio — Claude Code Specific Guidance

> **Audience:** Claude Code (the Anthropic agentic coding tool) when working on the Promissio codebase.
> **Status:** Living document.
> **Relationship to AGENTS.md:** This file is **additive**. Always read `AGENTS.md` first — it contains all general rules. This file only covers Claude Code specific patterns.

---

## How to use this file

`AGENTS.md` defines what Promissio is, its domain rules, coding standards, and what to never do. Those rules apply to you the same way they apply to any other AI agent.

This file covers:

- How to use Claude Code's specific capabilities effectively on this project.
- Workflows that work well in this codebase.
- Project-specific commands and shortcuts.
- Pitfalls that come from Claude's defaults that need adjustment for Promissio.

If anything in this file conflicts with `AGENTS.md`, **`AGENTS.md` wins**.

---

## 1. Operating Principles for Claude Code on This Project

### Read first, edit second

Before any non-trivial edit:

1. View the file you are about to change.
2. View neighboring files in the same folder to understand local conventions.
3. View the relevant ADR in `/docs/adr/` if one exists.
4. View the relevant domain doc in `/docs/domain/` if one exists.

Claude's default tendency to start editing quickly is wrong for this project. Take the extra few tool calls to read context. It saves much more time downstream.

### Use plan mode for non-trivial work

For any change that touches more than one file or requires architectural judgment, write out the plan first. The plan should be:

- A numbered list of concrete steps.
- Short — bullets, not paragraphs.
- Honest about what is uncertain.

Only proceed to edits once the plan is clear. If the user is present, get explicit confirmation on the plan for anything touching domain logic, financial math, or security.

### Use TodoWrite for multi-step tasks

For any task with three or more distinct steps, use the TodoWrite tool to track progress. This is not bureaucracy — it is a working memory aid that helps both you and the human user see where you are.

Mark items in_progress when starting, completed when done. Never mark something completed unless tests pass and the change is verified.

### Be precise about uncertainty

When you do not know something, say so explicitly. Phrases like "I think this might be" are useful. Phrases like "this should work" without verification are not.

For banking semantics specifically: never say "this is correct" unless you have verified against a reference source. Say "this matches the reference in X" or "I have not verified this — please review."

---

## 2. Tool Usage Patterns

### File reading

- For unfamiliar files, view the full file first. The truncation default is fine for already-seen files but loses context for new ones.
- For large generated files (EF migrations, OpenAPI spec output), view ranges, not the full file.
- When searching the codebase, prefer `Grep` for code patterns and `Glob` for finding files by name. Avoid bash `find` and `grep` — the dedicated tools are faster and more reliable.

### File editing

- Use `Edit` (str_replace) for surgical changes.
- Use `Write` (create_file) for new files or full rewrites.
- Never use `Write` to "edit" an existing file by rewriting it from scratch. That loses git history granularity and risks introducing unintended changes.
- When the `old_str` in an Edit might be ambiguous, include 2-3 lines of context above and below.

### Bash usage

- Use bash for: running tests (`dotnet test`), building (`dotnet build`), formatting (`dotnet format`), git operations.
- Do not use bash for file operations that have dedicated tools (reading, finding, editing).
- Always check the working directory before running build/test commands. Promissio's tests live in `tests/` — running `dotnet test` from the repo root tests everything; from a specific test project tests only that project.

### Test execution

- After any change to `Promissio.Domain` or `Promissio.Application`, run the corresponding test project.
- After changes to multiple projects, run `dotnet test` from the repo root.
- Integration tests require Docker. Check `docker ps` before assuming PostgreSQL is available.
- AI evaluation tests are slower and cost money (LLM API calls). Run them only when AI code changes, not on every refactor.

### Web search

- Use sparingly. Most banking and .NET answers should come from the codebase, documentation in `/docs/`, or the libraries' own docs.
- When you do search: cite the source in the code change. Don't rely on a memory of a search result.
- For current banking regulations: prefer EUR-Lex (official EU legal texts), ESMA, EBA, BIS sources. Avoid blog posts as primary references for legal rules.

---

## 3. Project-Specific Workflows

### Adding a new domain event

1. View existing events in `Promissio.Domain/Events/` to match conventions.
2. Create the event as a `record` in past tense, inheriting from `DomainEvent` base.
3. Update the aggregate to emit it from the relevant command.
4. Update any Marten projection that consumes events.
5. Add tests covering the emission.
6. Update `/docs/domain/events.md` with the new event.

### Adding a new MCP tool

1. View existing tools in `Promissio.AI.McpServer/Tools/`.
2. Define the tool with explicit parameter and return types.
3. Implement authorization check (no anonymous tools).
4. Add structured logging.
5. Add an entry in `/docs/mcp/tools.md`.
6. Add an evaluation case in `Promissio.AI.Evals` if the tool is used by an agent.
7. Manually verify the tool works from Claude Desktop.

### Refactoring a value object

Value objects are foundational. Refactoring requires care.

1. Read the existing tests to understand current contracts.
2. Make the change.
3. Run property-based tests — failures here usually indicate an invariant break.
4. Run all dependent code's tests.
5. Update any ADR that referenced the previous design.

### Adding an interest calculation feature

This is the highest-risk type of change in the project. The full protocol:

1. Find the authoritative reference (ISDA, EU directive, IFRS document).
2. Cite the reference in code and tests.
3. Implement the calculation.
4. Write reference test cases — at least 20 from the source document.
5. Add property-based tests for invariants (monotonicity, conservation, etc.).
6. Run mutation testing with Stryker.NET on the new code. Aim for 80%+ score.
7. Get human review before merging.

### Updating a state machine

1. Update the diagram in `/docs/domain/state-machines.md` first.
2. Update the state transition code.
3. Add tests for the new transitions (valid and invalid).
4. Verify no orphan transitions exist.
5. Cross-reference with ADRs.

---

## 4. Working with the Existing Plans

### `developers_plan.md`

This is the project roadmap, organized in phases. Before starting work on something new:

- Identify which phase the work belongs to.
- Check whether prerequisites in earlier phases are complete.
- If you are jumping ahead of the roadmap, flag this to the user — it usually indicates the plan needs an update or the work should wait.

### `FRONTEND_PLAN.md`

The frontend is intentionally minimal. If asked to build UI:

- For Layer 1 work (MCP server, banker scenarios, documentation), proceed normally.
- For Layer 2 work (Next.js AI Workspace), confirm scope first. The frontend has its own AGENTS.md in `frontend/` once that project is bootstrapped.

### ADRs

When making an architectural choice that's not already documented:

- Write the ADR first, as a draft.
- Discuss with the user.
- Once accepted, implement.
- Mark the ADR accepted.

---

## 5. Communication Style

### Brevity

For confirmations, acknowledgments, and simple questions: 1-2 sentences.

For technical explanations: as long as needed, no longer. Avoid restating context the user already has.

For code changes: minimal narration. The diff is the explanation.

### Tone

- Direct. Not curt, not effusive.
- Honest about uncertainty.
- No flattery ("great question") or padding ("certainly," "of course").
- Respectful disagreement when you have grounds for it.

### Pushback

When the user asks for something that conflicts with `AGENTS.md` or with this project's standards: say so clearly, explain the conflict, and propose an alternative. Do not silently comply with requests that would damage code quality or domain correctness.

Example: if asked to "just use System.DateTime here, it's simpler" — refuse, explain that NodaTime is required in domain code, and offer to do the conversion at the right boundary.

### Avoid sycophantic patterns

Do not start responses with "Great question!" or similar. Do not end with "Let me know if you need anything else!" The user will ask if they need something. Just answer.

---

## 6. Slash Commands (if Configured)

The project may include custom slash commands in `.claude/commands/`. Common ones to consider creating:

- `/test-domain` — runs only domain tests with verbose output.
- `/format-and-test` — runs `dotnet format` then `dotnet test`.
- `/adr-new` — creates a new ADR from template.
- `/eval-ai` — runs the AI evaluation suite.
- `/check-stack` — verifies no banned dependencies were added.

If these commands exist, prefer them over composing the equivalent bash inline.

---

## 7. MCP Integration

This project itself includes an MCP server (`Promissio.AI.McpServer`). When the project's MCP server is configured in your environment, you have direct tool access to loan data — use it for exploration.

When working on the MCP server itself:

- Test changes by manually invoking the tools from Claude Desktop, not just by running unit tests.
- Watch the structured logs in real time during testing.
- Verify authorization works as expected by switching identities.

---

## 8. Common Pitfalls Specific to This Project

### Pitfall 1: Reaching for `System.DateTime`

You will be tempted, because most .NET code in the wild uses it. **Do not.** Use NodaTime types everywhere in domain code. If you find yourself converting back and forth at every boundary, the boundary is in the wrong place.

### Pitfall 2: Treating `decimal` as money

`decimal` is a primitive. `Money` is a domain concept. Domain code expects `Money`. If you find yourself writing `decimal amount` in a domain method signature, stop and use `Money`.

### Pitfall 3: Plausible-but-wrong financial math

When asked to implement an interest calculation or schedule generation, you may produce code that looks reasonable but has subtle errors: wrong rounding direction, off-by-one day count, wrong period handling for grace periods, incorrect compounding frequency.

**Always verify against reference values from authoritative sources before claiming a calculation is correct.** Three to five reference cases minimum. The cost of getting this wrong in a portfolio project is much higher than the cost of double-checking.

### Pitfall 4: Mocking what you should integration-test

The default LLM instinct is to write unit tests with mocks. For Promissio:

- Domain logic: unit test, no mocks needed (pure logic).
- Repository code: integration test against real PostgreSQL via Testcontainers.
- API endpoints: integration test via WebApplicationFactory + Testcontainers.
- AI agents: evaluation test against actual model (using sandboxed test API key).

Resist the urge to mock EF Core, mock Marten, or mock the LLM. Those mocks teach you nothing and hide real bugs.

### Pitfall 5: Silent assumption about timezones

Banking operations happen in business days, in specific time zones. "End of day" depends on jurisdiction. Never assume UTC. Always use NodaTime's `DateTimeZone` explicitly when business semantics matter.

### Pitfall 6: Generating documentation that overstates completeness

When asked to write or update documentation, document what exists, not what is planned. Do not write "Promissio supports X" if X is in a future phase. Use clear markers for status: "Implemented", "Planned (Phase N)", "Under consideration".

### Pitfall 7: Accepting too-large tasks

If the user asks for "build the loan aggregate" — that's not one task, it's a phase. Break it down:

- List the value objects needed.
- List the state machine transitions.
- List the commands and events.
- List the invariants.
- Propose an order.

Then implement piece by piece, with tests at each step.

### Pitfall 8: Ignoring failed tests

If a test fails after your change, the default reaction is to investigate, not to delete the test. Even if the test "looks wrong," ask the user before changing it. Test code is often more carefully thought out than its style suggests.

---

## 9. When to Ask vs When to Decide

### Decide independently

- Naming of internal variables, local helpers.
- Formatting decisions covered by `dotnet format`.
- Test scaffolding for clear specifications.
- Adding obvious missing null checks or input validation.
- Refactoring within a single method for clarity.

### Ask first

- Anything in `Promissio.Domain` that involves financial calculations.
- Adding a new dependency.
- Changing a public API signature.
- Modifying or deleting any test.
- Architectural changes that span multiple projects.
- Any change to security or compliance logic.
- Anything that would conflict with `AGENTS.md`.

When in doubt, ask. Asking is cheap; reverting bad code is expensive.

---

## 10. Working Across Sessions

Claude Code does not have persistent memory between sessions. This means:

- At the start of each session, re-read `AGENTS.md` and this file.
- For multi-session work, leave clear pointers in commit messages, ADRs, and `/docs/` rather than relying on chat history.
- If a task spans multiple sessions, the previous session's todo list, current state, and next steps should be captured in a place that persists (e.g., a working notes file in `/docs/working-notes/` that is gitignored).

---

## 11. Honesty Checklist Before Submitting Work

Before declaring a task complete, verify honestly:

- [ ] All tests pass (`dotnet test`).
- [ ] Code is formatted (`dotnet format`).
- [ ] No `null!`, no `dynamic`, no `decimal` in domain APIs.
- [ ] No `System.DateTime` in domain code.
- [ ] New domain logic has unit tests.
- [ ] New public APIs have XML doc comments.
- [ ] Relevant documentation is updated.
- [ ] Commit messages follow Conventional Commits.
- [ ] No secrets, no PII in source or logs.
- [ ] Banking semantics verified against a reference source (if applicable).
- [ ] No `// TODO` or `// FIXME` left without an issue link.

If any item is unchecked, either complete it or flag it explicitly in your handoff.

---

*End of Claude Code guidance. Updates require human approval via PR.*
