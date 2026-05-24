# Phase 7 — AI Agents (Weeks 16–18)

> **Companion:** всегда подгружать вместе с `00-core.md`.
> **Weeks:** 16, 17, 18
> **Goal:** Build production-quality agents demonstrating mature AI engineering patterns in a regulated context.

---

## Week 16 — Credit Decisioning Copilot

### Tasks

1. Build a Semantic Kernel agent that assists underwriters in credit decisions.
2. Implement RAG over mock internal credit policies stored in Qdrant.
3. Provide tool calls to internal scoring service.
4. Produce structured output: decision recommendation, reasoning, identified risk factors.
5. Build a golden dataset of 30+ scenarios with expected decisions.
6. Implement evaluation metrics: decision accuracy, reasoning quality (LLM-as-judge).

### Acceptance criteria

- Evaluation suite runs in under five minutes.
- Decision accuracy on golden dataset exceeds 85%.
- Reasoning quality (judged by Claude or GPT-5) exceeds 4 out of 5 average.

---

## Week 17 — Early Warning Agent

### Tasks

1. Build an agent that monitors active loans and detects deterioration signals.
2. Combine hard data (payment behavior trends) with soft signals (mock customer communications sentiment).
3. Generate prioritized alerts for credit officers.
4. Build a simulated portfolio dataset and evaluate alert quality.

### Acceptance criteria

- Agent correctly identifies at least 80% of deteriorating loans in the simulated portfolio.
- False positive rate is below 15%.

---

## Week 18 — Collections Conversation Agent

### Tasks

1. Build a multi-turn conversational agent for outbound collections (chat-based, not voice).
2. Implement compliance constraints: no threats, no improper third-party disclosure, no after-hours contact, mandatory disclosures.
3. Maintain conversation memory across turns.
4. Build evaluation suite covering conversational quality, empathy, and compliance violation detection.

### Acceptance criteria

- Compliance violation rate in evaluation scenarios is zero.
- Conversational quality (judged by independent LLM) exceeds 4 out of 5 average.

---

## AI delegation notes

Prompt engineering is iterative and best done interactively with the actual model. The author defines compliance constraints and evaluation criteria. AI assistance helps with structuring agent code, integration with the MCP server, and writing evaluation scaffolding.
