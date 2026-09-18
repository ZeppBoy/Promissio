# Promissio development with Qwen3.8-27B-IQ3_M

Architecture and developer handoff, 2026-09-10. Runtime confirmed by owner: llama.cpp. GPU, VRAM, RAM, runtime revision, GGUF publisher and configured context are not yet known. This document prepares implementation work; it does not install a model or implement loan workflows.

Use Qwen as the implementation developer for bounded tasks. The owner/architect supplies accepted domain contracts and reviews results. This is a development-tool choice, not a replacement for Promissio's locked application AI providers.

## 1. Model and quantization implications

The official Qwen3.8-27B card describes a dense 27B model, native 262,144-token context and configurable thinking. Its published scores describe its evaluation setup, not this local IQ3_M deployment. Start from its documented thinking sampling profile: temperature 1.0, top_p 0.95, top_k 20, min_p 0, presence penalty 0 and repetition penalty 1.0. Thinking effort defaults to xhigh; medium is an experiment to compare, not an assumed improvement. [Official model card](https://huggingface.co/Qwen/Qwen3.8-27B).

IQ3_M is a low-bit weight quantization recipe, not a smaller parameter-count model or a context-window setting. Different publishers can use different conversions, calibration and tensor precision. Bartowski lists its specific IQ3_M file at 13.90 GB and characterizes it as medium-low quality comparable to Q3_K_M. This is a publisher assessment, not measured Promissio coding accuracy. [Quantization publisher](https://huggingface.co/bartowski/Qwen3.8-27B-GGUF).

Weight compression is lossy. Its effect on this codebase must be measured; do not invent a percentage accuracy loss. It does not reduce the precision of decimal arithmetic in the C# program once correct code is compiled. The risk is generating incorrect code, missing constraints or selecting the wrong API. More thinking tokens cannot restore the original weights. Fluent explanations and passing self-authored tests are insufficient evidence of financial correctness.

The following limits are architect-selected starting policies, not discovered model limits:

| Constraint | Starting policy | Reason |
|---|---|---|
| Work unit | One behavior, one layer, normally 1–3 production files | Makes cross-file omissions visible |
| Patch size | Aim below 150 changed production lines; split before 250 | Review and repair remain manageable |
| Active work | One ticket and one writer | Avoids contradictory edits |
| Context | Trial 16K, then 32K only if memory and qualification permit | Establish a measured working envelope |
| Input budget | At most half the configured context initially | Reserve space for reasoning, tool results and patch output |
| Output | Trial 4K–8K tokens within the remaining context | Detect truncation; reduce task size when insufficient |
| Repair | At most two unsuccessful repair attempts on the same cause | Stop blind edit loops and seek diagnosis |
| Verification | Compiler, independent reference cases, tests and reviewer | Model confidence does not authorize completion |

Do not truncate AGENTS.md to fit a task. Load it fully at session start, then add only the accepted ADRs and exact source needed. If the package exceeds the budget, shrink the task or use a qualified larger context. Do not dump the historical roadmap, all audits or the whole repository into each prompt. Start fresh between tickets and reload authoritative instructions and current files; never depend on an earlier conversational summary as a contract.

## 2. llama.cpp qualification before edits

Record an environment sheet outside secrets: GGUF repository/revision, filename and SHA-256, whether the weights are original or modified, llama.cpp build/commit, GPU and driver, VRAM/RAM, GPU offload, context, parallel slots, cache types, chat template, sampling settings and harness version. Do not substitute an uncensored or otherwise modified model because its filename looks similar.

Use the model's compatible embedded template and confirm its behavior with the selected llama.cpp build. llama.cpp exposes template, reasoning, context and tool-call controls; the exact supported options must come from that binary's help. Tool calling needs a compatible parser/template and must be exercised end-to-end. [llama.cpp server documentation](https://github.com/ggml-org/llama.cpp/blob/master/tools/server/README.md).

Recommended setup sequence:

1. Inspect `llama-server --version` and `llama-server --help`. Record both. Verify that the build loads this GGUF successfully before tuning.
2. Start on localhost, one parallel slot, text-only work. Use the actual GGUF path, not a guessed download alias. Keep MTP/speculative decoding off until baseline reliability is measured.
3. Trial a 16K context if it fits. Total memory includes resident weights, cache/recurrent state, compute buffers and runtime overhead. The 13.90 GB file size is not a VRAM requirement; no GPU-fit or tokens/second promise is made here. Inspect load logs and peak memory. CPU offload is an option to measure, not a guaranteed speedup.
4. Start with runtime-default cache precision. Weight IQ3_M and KV-cache quantization are independent changes; do not introduce both at once. If memory fails, reduce context or offload, then repeat qualification.
5. Use the official thinking sampling profile above. Confirm how the actual template maps reasoning effort and preserved thinking. Do not replace the profile with greedy decoding as an assumed coding fix.
6. Use a coding client that sends genuine system/user/tool messages, retains tool-call IDs/results and reports process exit codes. The server alone cannot inspect or edit this repository. If no tool-capable client exists, use patch-only output applied and tested by the human.
7. Confirm that reasoning text is separated from patch/tool content. Never execute a shell command extracted from free-form reasoning. Treat a response cut off at its token limit as incomplete.
8. Run the qualification tasks below before permitting implementation. Pin the successful settings. Requalify after changing model, template, runtime, cache precision or context policy.

Illustrative launch shape, to fill in after inspecting the installed binary (PowerShell):

```powershell
$ggufPath = '<absolute path to the verified GGUF>'
llama-server --model $ggufPath --host 127.0.0.1 --port 8080 --ctx-size 16384 --parallel 1 --jinja --temp 1.0 --top-p 0.95 --top-k 20 --min-p 0 --presence-penalty 0 --repeat-penalty 1.0
```

This is a provisional command, not a hardware-tuned configuration. Leave GPU/cache flags at verified runtime defaults until the environment sheet is complete. Do not assume the example is compatible with an old build.

## 3. Qualification and acceptance

Run ten small fixtures, three fresh sessions each, against a disposable checkout. The architect owns expected outcomes before generation. Include: locating an existing API; identifying a proposed ADR; producing a parseable tool call; respecting a file allowlist; documenting an existing public member; making an isolated validator change from a supplied contract; adding a supplied negative test; rejecting an unsupported financial expected value; responding correctly to a compiler error; and reporting an intentionally failing test honestly.

Fixtures that modify code need concrete pre-approved synthetic inputs and assertions. Never create financial reference values by asking Qwen to calculate them. Capture first-attempt and final results separately, changed files, repair count, elapsed time, token usage, peak memory and any malformed tool calls or truncated output. Keep review judgments separate from deterministic checks.

Proposed admission gate: at least 27/30 successful fixture runs, zero unauthorized edits or fabricated verification claims, and all financial/approval-boundary fixtures correct. These are local policy thresholds, not a research benchmark or a safety guarantee. Any boundary failure requires correction and requalification. Do not run the ten-fixture experiment on production data.

If quality is inadequate, first inspect template/tool parsing and missing context, then reduce ticket size. If hardware permits, compare the same base revision at Q4_K_M or Q5_K_M with identical fixtures/settings; change one variable at a time. Retain IQ3_M only if its measured quality and resource tradeoff satisfy the gate. No alternate download or hardware purchase is authorized by this plan.

## 4. Delivery sequence

**Sequencing update:** on 2026-09-10, the owner deferred the remaining Phase 2 assurance work while Phase 3 contract preparation proceeded. On 2026-09-18, Phase 2 implementation and automated gates were completed and the owner signed off its financial contracts. Q05–Q12 still require their accepted contracts. The sequence below describes dependencies and retained work, not permission to override these decisions.

Baseline main commit: 8b3c314. Prior evidence: 405 tests passed, with empty Application, Integration and AI evaluation projects. Rerun against the actual checkout; a historic count is not a current result. Read [delivery status](../../status.md) and [verification](../../verification/README.md).

| Order | Work | Can Qwen begin? | Completion gate |
|---|---|---|---|
| Q00 | Model/runtime qualification | Yes, in disposable checkout | Environment and 30 trial results reviewed |
| Q01 | Repository baseline and Phase 2 assurance inventory | Yes, read-only analysis plus report | Existing verification and reference coverage mapped |
| Q02 | Coverage measurement setup | After concrete tool/package choice is reviewed | Reproducible measured report; no exclusions hiding financial code |
| Q03 | Mutation baseline | After Q01; installed tool available | Score/report recorded, surviving mutants classified |
| Q04 | Close one assurance gap per ticket | After architect supplies exact expected behavior/source | Meaningful regression/reference test and review |
| D01 | Resolve Phase 3 contracts | Owner/architect decision; Qwen may gather evidence | ADRs 0006/0007 accepted, transition/event contracts approved |
| Q05 | One domain event/value contract | After D01 | Approved fields/invariants, XML docs and tests |
| Q06 | Aggregate creation and replay foundation | After Q05 | Valid/invalid creation and replay equivalence tests |
| Q07 | One approved state transition per ticket | After Q06 | Positive/negative transition cases and matching diagram |
| Q08 | One Application command and persistence port | After related transition | Handler tests, cancellation and expected errors |
| Q09 | One Marten storage operation | After persistence contract | Real PostgreSQL tests for append, load and conflict |
| Q10 | Idempotent workflow | After atomicity contract | Duplicate, changed-payload replay and crash/retry tests |
| Q11 | One projection, then historical queries | After query/time semantics approval | Rebuild equivalence and at least 10 historical scenarios |
| D02 | Resolve authorization | Owner/security reviewer | ADR 0008 and operation permission matrix accepted |
| Q12 | One authorized HTTP operation | After D02 and tested workflow | Happy path plus three failures, including access denial |
| Later | Batch, MCP, AI and launch phases | Separate bounded task packets | Existing phase acceptance criteria, never scaffold counts |

Do not jump to all of Phase 3 in one prompt. Q05–Q12 are a sequence for future task packets, not permission to invent contracts. Event-time versus business-effective-time semantics must be decided before historical queries. The handoff milestone, identifiers, monetary allocation, reversal policy and transactional behavior must be explicit where relevant.

Phase 2 gate: Domain line coverage >=90% and mutation score >=80%, independently reviewed reference cases and no unresolved correctness failures. Application >=80% applies when workflows exist. A high score cannot compensate for incorrect assertions. Do not weaken thresholds or remove tests to advance a ticket.

## 5. Instructions and review

Use [developer instructions](developer-instructions.md) as the recurring work policy and [task packets](task-packets.md) as the ticket format. Fill every required contract field before issuing an implementation ticket. Keep code, tests and corresponding documentation together.

After each ticket: inspect scope, run focused tests, run the shared verification script and explicitly format/check newly changed paths because current CI formatting scope is limited. Keep changes uncommitted unless the task authorizes a commit; push only with an explicit instruction for that work. Earlier permission to push a previous change is not a standing deployment policy.

Generated documentation is ready for human editing, not marked human-reviewed. This plan leaves existing AGENTS.md and proposed ADR statuses unchanged.
