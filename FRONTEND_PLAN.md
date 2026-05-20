# Promissio — Frontend Plan

> **Status:** Draft v1.0 — Living document.
> **Owner:** Project lead
> **Last updated:** 2026-05-17
> **Companion to:** `developers_plan.md` — read that first for project context.

---

## Table of Contents

1. [Mission and Philosophy](#1-mission-and-philosophy)
2. [Two-Layer Strategy](#2-two-layer-strategy)
3. [Layer 1 — Banker Workstation via Claude Desktop](#3-layer-1--banker-workstation-via-claude-desktop)
4. [Layer 2 — AI Workspace Frontend](#4-layer-2--ai-workspace-frontend)
5. [Integration with Backend](#5-integration-with-backend)
6. [Design Principles](#6-design-principles)
7. [Working with AI Coding Agents](#7-working-with-ai-coding-agents)
8. [Demo and Launch Materials](#8-demo-and-launch-materials)
9. [Non-Goals](#9-non-goals)
10. [Appendix — Reference Materials](#10-appendix--reference-materials)

---

## 1. Mission and Philosophy

Promissio is a backend-first project. The main developers plan (`developers_plan.md`) explicitly defines a UI as a non-goal, because Staff Engineer signal comes from domain depth, distributed systems patterns, and AI engineering — not from frontend craft.

However, two visual surfaces are necessary:

1. **A way for a real user (a credit officer) to interact with the system in demos and live walkthroughs.** Without this, the project remains an abstract API exercise with no story for non-engineers.
2. **A modern, AI-native showcase** that demonstrates an understanding of how regulated software is being reimagined in 2026 — agent-driven, transparent, streaming, generative.

These two needs are deliberately solved separately, by two layers that target different audiences and require different investment.

### What this plan rejects

- A traditional banking dashboard with tables, filter panels, sidebar navigation, and modal forms. This is a solved problem from 2010 and adds zero signal for any role Promissio targets.
- Custom-built CRUD screens for every backend endpoint. The OpenAPI spec via Scalar already serves this purpose for engineers.
- A mobile app, native client, or progressive web app. Out of scope.
- Building or shipping any design system from scratch. We compose existing, well-maintained primitives.

### What this plan emphasizes

- **Agentic UX over form-based UX.** The credit officer's primary interface is a conversation with an AI agent that has tools, memory, and reasoning — not a series of forms.
- **Transparency by construction.** Every tool call, every retrieval, every state transition is visible. This is non-negotiable in a regulated context.
- **Streaming-first interaction.** Long-running operations stream their progress. Nothing blocks.
- **Generative UI where it earns its keep.** The agent renders structured outputs (schedules, charts, restructuring options) into interactive components, not opaque chat bubbles.

---

## 2. Two-Layer Strategy

### Layer 1 — Banker Workstation via Claude Desktop (Primary)

Banker uses **Claude Desktop** as their workstation. Promissio exposes itself entirely through the **MCP server** built in Phase 6 of the main plan. No custom UI is required.

**Why this works as the primary interface:**

- Claude Desktop is already a polished, accessible, multi-platform client. It handles streaming, tool calls, attachments, conversation history, search, model selection, and authentication.
- This is the most authentic demonstration of where banking software is heading: away from custom dashboards, toward AI assistants with the right tools.
- Zero frontend code to maintain. Every improvement to Claude Desktop is a free upgrade.
- Strongest signal for AI-savvy hiring managers and conference audiences.

**Investment:** approximately 1–2 weeks of documentation, demo content, and configuration work. No production code beyond what already exists in the MCP server.

### Layer 2 — AI Workspace Frontend (Showcase)

A minimal, modern web application built specifically as a portfolio showcase. Demonstrates that Promissio could expose its own polished, agent-driven UI for organizations that cannot or will not deploy Claude Desktop to bankers.

**Why this matters in addition to Layer 1:**

- Some viewers (recruiters, LinkedIn audiences, conference attendees in less AI-fluent venues) need to see a "real" UI to feel that the project is complete.
- LinkedIn videos and demo reels look more impressive with a custom interface than with a Claude Desktop screen.
- It demonstrates understanding of modern frontend craft and AI UX patterns from 2026 — a relevant skillset even for backend-focused Staff Engineers.

**Investment:** 3 weeks of focused frontend work, kept deliberately narrow in scope.

### When each layer is shown

| Audience | Primary surface |
|---|---|
| AI engineers, AI-savvy hiring managers | Layer 1 (Claude Desktop) — most authentic |
| Banking domain practitioners | Layer 1 (Claude Desktop) — closer to how they actually work |
| Generalist recruiters, LinkedIn audience | Layer 2 (AI Workspace) — more visually digestible |
| Conference attendees | Both, depending on audience and talk angle |
| Engineering manager portfolio review | Layer 1 demonstrates depth; Layer 2 confirms breadth |

---

## 3. Layer 1 — Banker Workstation via Claude Desktop

### Concept

A senior credit officer at a regional bank opens Claude Desktop in the morning. They ask Claude to summarize overnight portfolio changes, flag any loans that crossed into Stage 2, draft a restructuring proposal for a specific customer, and prepare a delinquency report. Claude does all of this by calling tools exposed by the Promissio MCP server.

No application installation, no custom dashboard, no login flow specific to Promissio. The banker authenticates once to Claude Desktop and once to the Promissio MCP server.

### Required deliverables

#### 1. Comprehensive MCP server documentation

Location: `/docs/mcp/`

Contents:

- `setup.md` — how to install the Promissio MCP server, configure connection strings, set up authentication, and connect Claude Desktop to it. Step-by-step, screenshot-supported.
- `tools.md` — exhaustive reference for every tool exposed: name, parameters, return shape, security boundaries, example invocations.
- `usage-patterns.md` — common banker workflows and how they map to tool sequences.
- `compliance.md` — what the MCP server does and does not allow, audit logging behavior, PII handling.

#### 2. Reference Claude Desktop configuration

A ready-to-use `claude_desktop_config.json` snippet that connects to a local Promissio MCP server. Documented for macOS, Windows, and Linux paths.

#### 3. System prompt template for banker persona

A polished, production-grade system prompt that:

- Defines the banker's role and authority limits.
- Instructs Claude on which tools to prefer for which questions.
- Sets compliance constraints (no threats, no third-party disclosure, no after-hours scheduling).
- Establishes the conversation tone (professional, empathetic, factual).
- Includes few-shot examples of correct tool sequencing for common queries.

#### 4. Banker workflow scenarios

Eight to ten scripted scenarios that demonstrate Promissio's capabilities through Claude Desktop. Each scenario:

- Has a clear business question (e.g., "How is the loan portfolio performing this month?").
- Shows the expected conversation, including tool calls.
- Highlights which Promissio features are exercised.

Suggested scenarios:

1. **Morning portfolio briefing.** "What changed in my portfolio overnight?"
2. **Single loan deep dive.** "Show me everything about loan #LN-2026-0042."
3. **Delinquency triage.** "Which loans crossed into Stage 2 this week and why?"
4. **Restructuring proposal.** "Customer X lost their job and asks for restructuring. What are reasonable options?"
5. **Payoff calculation.** "What does customer Y need to pay to close the loan on December 31?"
6. **Early warning investigation.** "Why did the early warning agent flag loan #LN-2026-0167?"
7. **Compliance check.** "Generate a draft payment reminder for loan #LN-2026-0042 that complies with our policies."
8. **Portfolio analytics.** "What is our weighted average APRC for active consumer loans, broken down by origination quarter?"
9. **Counterfactual analysis.** "If we offered customer Z a 3-month payment holiday, what would the new schedule look like?"
10. **Audit trail retrieval.** "Show me every change to loan #LN-2026-0042 since origination."

#### 5. Demo video — banker workflow

A 5-to-7-minute screen recording showing a banker using Claude Desktop with Promissio MCP server to handle a realistic Monday morning. Voiceover explains both what the banker is doing and what is happening under the hood (MCP tool calls, agent reasoning).

Production quality: clean audio, screen recording at 1080p minimum, simple intro/outro card, captions for accessibility.

Hosted on YouTube, linked from the main README, embedded in launch blog posts.

#### 6. Demo data

A `seed-data/` directory with a realistic portfolio of 100–200 loans in various states (active, past due, defaulted, restructured) plus accompanying customer records. Reproducible via a single command: `dotnet run --project tools/SeedDemoData`.

### Acceptance criteria for Layer 1

- A new developer can clone the repository, run `docker compose up`, run the seed-data tool, configure Claude Desktop, and execute the first banker workflow scenario end-to-end in under fifteen minutes.
- All ten banker workflow scenarios complete successfully against the seeded portfolio.
- The demo video is recorded, published, and linked from the main README.
- All MCP server documentation pages are written and reviewed.

### AI delegation notes for Layer 1

- Documentation drafts can be generated by Claude based on the actual MCP server code and tool schemas. The author edits for accuracy, tone, and completeness.
- Demo data generation is well-suited to AI assistance using Bogus and realistic distributions.
- Video script can be drafted by Claude; the author records voiceover and editing personally — authenticity matters more than polish.
- System prompt template is iterated with Claude through actual conversations and refined based on observed behavior.

---

## 4. Layer 2 — AI Workspace Frontend

### Concept

A web application that recreates the Claude Desktop banker experience inside a Promissio-branded UI, with deeper integration into the platform. Not a dashboard. A **workspace**.

The main screen is a conversation. The agent has tools. The user sees those tools being called. The agent can render structured outputs as interactive components inline. There is a persistent context panel showing the current case being worked on (loan, customer, application). There is no menu of forms.

This is what a credit officer's interface looks like in 2026 if designed today.

### Reference inspirations

- **Claude.ai web interface** — for streaming patterns, tool call visualization, artifact rendering.
- **Cursor** — for the way it surfaces agent reasoning, tool use, and edits in a clean sidebar.
- **Linear** — for keyboard-first, fast interaction patterns, command palette.
- **ChatGPT Canvas** — for the side-by-side document/conversation pattern.
- **Vercel v0** — for generative UI patterns where the agent produces React components.
- **Anthropic Console** — for clean inspector views over tool calls and traces.

### Technology stack

**Framework:** Next.js 15 with App Router and React Server Components.

**Language:** TypeScript, strict mode.

**Styling:** Tailwind CSS v4 with shadcn/ui as the component primitive library. No custom design system; we compose well-tested primitives.

**AI integration:** Vercel AI SDK 4.x for streaming, tool calls, and generative UI patterns. The SDK is the de facto standard for streaming agentic UIs in TypeScript in 2026.

**State and data:** TanStack Query for server state, Zustand for client-side UI state. No Redux.

**Forms (where needed):** React Hook Form with Zod validators sharing schemas with the backend API contracts.

**Charts:** Recharts for simple visualizations, Tremor for richer financial charts where appropriate.

**Authentication:** OAuth 2.0 via Auth.js (NextAuth v5), with the same identity provider that protects the backend APIs.

**Build and deployment:** Vercel for development and preview deployments; self-hosting via Docker for production parity demos.

**Backend communication:**

- Promissio REST APIs for traditional reads and writes.
- Promissio MCP server is consumed by an in-process AI agent runtime (Vercel AI SDK with Anthropic provider), which calls MCP tools server-side.

### Information architecture

The application has three primary surfaces:

#### Conversation surface (the home view)

- Main pane: streaming chat with the credit officer's AI assistant.
- Left rail: conversation history, organized by case or by date.
- Right panel: contextual case panel showing the current loan, customer, or application under discussion. Updates dynamically as the conversation evolves.

#### Inspector view (developer/audit mode)

- Per-message inspector showing the full trace: which tools were called, with what arguments, what they returned, how long they took, what they cost.
- Toggleable from the conversation surface. Hidden for casual users, surfaced for compliance officers, auditors, and engineers.

#### Portfolio surface (minimal table-driven view)

- A single screen with a searchable, filterable list of loans for situations when the banker needs to scan, not converse.
- Clicking a loan opens the conversation surface with that loan as context.
- This surface exists because some operations are genuinely better served by a list than by a conversation. It is intentionally minimal.

### Key UX patterns

#### Streaming-first responses

Every agent response streams token by token. Tool calls appear in the stream the moment they begin, with a loading indicator, and are replaced by their results when complete.

#### Tool call transparency

Tool calls render as collapsible cards inline in the conversation:

```
┌──────────────────────────────────────┐
│ ⚙ get_loan_by_id                    │
│   id: "LN-2026-0042"                 │
│   ✓ Completed in 142 ms              │
│   ▾ Show result                      │
└──────────────────────────────────────┘
```

This is non-negotiable. In a regulated context, the banker must always be able to see what the agent did on their behalf.

#### Generative UI for structured outputs

When the agent returns structured data, it renders as a proper component, not a JSON dump:

- A payment schedule renders as a sortable table with monthly rows.
- A restructuring proposal renders as a comparison card showing original vs. proposed.
- A portfolio summary renders as a small dashboard with KPIs and a sparkline.

These components are reusable React components defined under `components/agent-outputs/` and selected by the agent through structured tool returns.

#### Compliance overlays

Sensitive operations (writing off a loan, restructuring, generating customer communications) require explicit confirmation through a modal that displays:

- What the agent proposes to do.
- Which policy provisions apply.
- The audit trail entry that will be created.
- Approve / Deny buttons.

The agent cannot complete write operations silently. Reads are unrestricted but logged.

#### Keyboard-first interaction

Cmd+K opens a command palette. The palette can:

- Start a new conversation about a specific loan.
- Jump to a recent case.
- Trigger a portfolio search.
- Open the inspector for the current conversation.

### Implementation phases (3 weeks)

#### Week 1 — Foundation and conversation surface

**Goal:** A working chat interface that streams responses from the agent and renders tool calls.

Tasks:

1. Scaffold Next.js 15 project with TypeScript, Tailwind, shadcn/ui.
2. Set up Vercel AI SDK with Anthropic provider and Claude Sonnet 4.7 as the primary model.
3. Implement the conversation surface layout: left rail, main pane, right panel.
4. Implement streaming chat with tool call rendering.
5. Wire the agent runtime to the Promissio MCP server (server-side, the frontend never talks to MCP directly).
6. Implement authentication via Auth.js.
7. Connect to Promissio REST APIs for non-conversational reads (portfolio surface stub).

Acceptance: a banker can authenticate, send a message, see streaming response with visible tool calls returning real data from the seeded portfolio.

#### Week 2 — Context panel, generative UI, and inspector

**Goal:** Rich, structured outputs and full transparency.

Tasks:

1. Implement the case context panel that updates as the conversation focuses on different loans, applications, or customers.
2. Build the five core generative UI components: payment schedule table, restructuring comparison card, portfolio summary panel, loan timeline, payment history list.
3. Wire the agent's structured tool outputs to render through these components.
4. Implement the inspector view showing full traces for any conversation message.
5. Implement the portfolio surface as a simple, searchable list with filters.

Acceptance: when the agent retrieves a payment schedule, it renders as an interactive table; when it proposes restructuring, it renders as a comparison card; inspector view shows complete tool call traces.

#### Week 3 — Compliance overlays, polish, and demo content

**Goal:** Production-quality polish and a launch-ready demo.

Tasks:

1. Implement compliance confirmation overlays for write operations.
2. Implement the command palette (Cmd+K).
3. Implement responsive layouts down to 1024px width (this is desktop software; mobile is out of scope).
4. Implement dark mode (shadcn/ui handles most of it, refinement required).
5. Apply visual polish: micro-animations, loading skeletons, empty states, error states.
6. Record demo video specifically for Layer 2.
7. Write Layer 2 documentation: `/frontend/README.md` with setup, architecture, and contribution guidelines.

Acceptance: the application can be cloned, configured, and run by a new developer in under ten minutes; demo video is recorded; all primary user flows have polished error and empty states.

### Acceptance criteria for Layer 2

- All three primary surfaces (conversation, inspector, portfolio) are functional and visually polished.
- Streaming and tool call rendering work reliably under realistic load.
- Generative UI components render correctly for at least five distinct output types.
- Compliance overlays gate all write operations.
- Demo video showcasing the AI Workspace is published and linked from the main README.
- Lighthouse accessibility score above 90 on the conversation surface.

### AI delegation notes for Layer 2

Frontend work is the strongest candidate for AI delegation in the entire project. Modern AI coding assistants (Claude Code, Cursor) handle React, TypeScript, Tailwind, and shadcn/ui composition reliably and often produce better code than the average human first draft.

**Delegate aggressively:**

- Component scaffolding for all UI primitives.
- Tailwind class composition and responsive variants.
- Form scaffolding with React Hook Form + Zod.
- Storybook stories if used.
- Test scaffolding for components.
- Translation of designs into shadcn/ui compositions.
- Accessibility refinements (ARIA, keyboard navigation).

**Own personally:**

- Information architecture and component composition decisions.
- The conversation surface interaction model.
- Tool call transparency UX.
- Compliance overlay flow design.
- Visual identity and aesthetic decisions.
- Performance budgets and bundle size monitoring.

---

## 5. Integration with Backend

### Authentication and authorization

A single identity provider (Auth0, Keycloak, or Microsoft Entra ID in development) protects:

- The Promissio REST APIs.
- The Promissio MCP server (via OAuth 2.1 with PKCE as the MCP specification requires).
- The Layer 2 frontend.

The frontend obtains tokens via the standard OAuth flow and forwards them when calling backend services. The MCP server validates the same tokens and enforces role-based access on every tool invocation.

### API contracts

Layer 2 consumes:

- **REST APIs** for read-heavy operations that are better served without an LLM: listing portfolio, fetching a single loan's static data, retrieving event history for audit views.
- **The MCP server**, indirectly, through the in-process Vercel AI SDK agent runtime that orchestrates Claude and tool calls.

The frontend never calls the MCP server directly from the browser. All agent orchestration happens server-side in Next.js API routes or Server Actions. This protects credentials, allows for rate limiting, and enables server-side audit logging.

### Streaming patterns

Vercel AI SDK provides primitives for:

- Token-by-token streaming via Server-Sent Events.
- Tool call streaming with intermediate state.
- Multi-step agent loops with visible reasoning.

The frontend reads these streams and renders accordingly. The backend is unaware of how the frontend chooses to display them.

### State synchronization

The application is intentionally not real-time-collaborative. A single banker per session. Server state is read on demand via TanStack Query with appropriate cache invalidation when the agent performs writes.

---

## 6. Design Principles

These principles guide every design decision in both layers.

### Agentic over form-based

If a task can be accomplished by a short conversation with an AI agent that has the right tools, it is accomplished that way. Forms exist only when the input is highly structured, frequently repeated, and not improved by conversation (e.g., a portfolio search filter).

### Transparency by default

Every agent action is visible. Every tool call shows its parameters and results. Every write operation requires explicit confirmation with full context. No hidden side effects.

### Streaming over blocking

Long-running operations stream their progress. The user always sees something happening. Spinners are a last resort.

### Generative UI over chat dumps

Structured data renders as interactive components, not as JSON in chat bubbles. The agent's outputs are first-class UI citizens.

### Compliance-aware UX

Regulated industries have non-negotiable requirements: audit trails, mandatory disclosures, right to explanation. These are not afterthoughts. They are designed into the primary interaction flow.

### Keyboard-first

Power users live on the keyboard. Cmd+K opens the command palette. Common actions have keyboard shortcuts. The mouse is optional, not required.

### Desktop-only

Banker workstations are desktops or large laptops. We do not build for mobile. We do not build for tablets. We design for 1280×800 minimum and optimize for 1920×1080.

### Accessible

WCAG AA compliance is the floor. shadcn/ui components meet most requirements out of the box. We refine, not retrofit.

---

## 7. Working with AI Coding Agents

### Layer 1 specifics

- Documentation generation: highly suitable for AI assistance. Author edits for accuracy and brand voice.
- Demo data seeding: highly suitable. The author specifies realistic distributions; the agent generates data.
- System prompt iteration: best done interactively in actual Claude conversations, not via code generation. Author drives this personally.

### Layer 2 specifics

Frontend work is where AI assistants currently provide the highest leverage. The author should plan for two to three times the productivity ratio of backend work.

**Workflow recommendations:**

1. Always start with shadcn/ui primitives. Do not let the agent invent custom components when a primitive exists.
2. Provide visual references. When asking for a component, link to inspiration (Linear, Cursor, Claude.ai). AI assistants produce significantly better designs when grounded in references.
3. Iterate in small steps. A single component at a time, reviewed and refined before moving on.
4. Verify accessibility manually. AI assistants often miss subtle ARIA requirements.
5. Maintain a tight `AGENTS.md` in the frontend directory with project-specific conventions: imports, component organization, state management patterns, naming.

### What never to delegate

- Information architecture decisions (what surfaces exist, how they relate).
- Interaction model design (how streaming, tool calls, and confirmations work together).
- Compliance flow design.
- Performance budgets.
- Bundle and dependency choices.

---

## 8. Demo and Launch Materials

The frontend layers exist primarily to enable compelling demos. Treat demo content as a first-class deliverable.

### Required artifacts

#### Layer 1 demo video (5–7 minutes)

Title: "Banking operations without a dashboard: Claude Desktop + Promissio MCP server."

Format: screen recording with voiceover. Shows a credit officer's morning routine through Claude Desktop, calling into the Promissio MCP server. Voiceover explains what is happening at the protocol level.

Distribution: YouTube, linked from README, embedded in LinkedIn launch post.

#### Layer 2 demo video (3–5 minutes)

Title: "What banking software looks like in 2026: AI Workspace for credit officers."

Format: screen recording with voiceover. Showcases the AI Workspace handling the same banker scenarios as the Claude Desktop video, but with the Promissio-native interface.

Distribution: same as Layer 1.

#### Architecture walkthrough video (10–12 minutes)

Title: "Building Promissio: an AI-augmented loan servicing platform in .NET."

Format: presentation-style with architecture diagrams, brief code walkthroughs, and demo footage. The flagship video for conference talk proposals.

Distribution: YouTube, README, conference submissions.

#### Screenshots

For LinkedIn, README, and blog posts:

- Three Layer 1 screenshots: Claude Desktop conversation with MCP tool calls visible, inspector view of trace, portfolio summary rendered by agent.
- Five Layer 2 screenshots: conversation surface, generative payment schedule, compliance overlay modal, inspector trace view, portfolio surface.

Captured at 1920×1080, high quality. Stored under `/docs/screenshots/`.

#### GIFs

Short animated GIFs of:

- The agent calling a tool and rendering a payment schedule.
- A compliance overlay flow from request to approval.
- The command palette in action.

Useful for LinkedIn and Twitter where embedded videos do not autoplay reliably.

### Launch sequencing

The frontend layers ship after the backend, not in parallel. Suggested launch sequence:

1. Backend Phases 0–6 complete, MCP server functional.
2. Layer 1 deliverables ship: documentation, demo data, system prompt, banker scenarios, Layer 1 demo video.
3. Backend Phases 7–10 complete: agents, evaluations, observability, production polish.
4. Layer 2 deliverables ship: AI Workspace frontend, Layer 2 demo video.
5. Public launch (Phase 10 of the main plan) uses all of the above.

---

## 9. Non-Goals

To keep both layers focused, the following are explicitly out of scope.

- **Native mobile apps.** Bankers work on desktops.
- **Offline support.** The system is fundamentally online (agents call cloud LLMs).
- **Multi-language internationalization.** English only. The codebase is documented in English, the UI is in English. Translation can be considered for year two.
- **White-label theming.** A single, opinionated visual identity. No theme builder.
- **A general-purpose admin panel.** No CMS-style screens for managing users, roles, configurations through the UI. Those operations happen via API or CLI.
- **Realtime collaboration.** No shared cursors, no presence indicators, no multi-user simultaneous editing of the same loan.
- **A customer-facing portal.** Promissio is an internal-facing platform. Customer-facing surfaces (web banking, mobile banking) are out of scope.
- **Print or PDF generation.** Reports can be exported as data; rendering them as styled PDFs is out of scope.

---

## 10. Appendix — Reference Materials

### Frontend frameworks and libraries

- **Next.js** documentation, especially the App Router and Server Actions sections.
- **shadcn/ui** component library — the source code is the documentation.
- **Vercel AI SDK** documentation and cookbook.
- **Tailwind CSS v4** migration guide and core concepts.
- **TanStack Query** v5 documentation.
- **Auth.js** v5 (NextAuth) for authentication.

### Design references

- **Linear** — for keyboard-first interaction patterns, command palette implementation, minimalist aesthetic.
- **Cursor** — for AI agent UX patterns, tool call surfacing, side panels.
- **Claude.ai** — for streaming, tool call rendering, artifact patterns.
- **Vercel v0** — for generative UI patterns.
- **Anthropic Console** — for inspector and trace visualization.

### AI UX writing

- Latent Space podcast episodes on agentic UX patterns.
- Vercel AI SDK blog posts on streaming UI and generative UI.
- Anthropic engineering blog on agent design patterns.
- Linear's public design philosophy.

### Compliance and regulated UX

- EU AI Act provisions on transparency and human oversight.
- DORA (Digital Operational Resilience Act) implications for financial UI.
- WCAG 2.2 AA compliance documentation.

---

*End of frontend plan. Updates discussed in pull requests; the document evolves with the project.*
