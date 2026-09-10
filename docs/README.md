# Documentation

Start with [current status](status.md), then [current architecture](architecture/current-state.md).

| Document family | Owns |
|---|---|
| [Roadmap](plan/README.md) | Future scope and phase acceptance criteria |
| [Architecture](architecture/README.md) | Current structure and target boundaries |
| [ADRs](adr/README.md) | Accepted decisions and proposals awaiting review |
| [Domain](domain/README.md) | Financial contracts, formulas and reference cases |
| [Verification](verification/README.md) | Commands, enforced checks and dated evidence |
| [MCP](mcp/README.md) | Implementation status and future client documentation |
| [Historical audits](audits/README.md) | Preserved observations about earlier code |
| [Original roadmap](archive/developers-plan-2026-05-17.md) | Historical planning snapshot, not current guidance |

## Ownership and maintenance

AGENTS.md remains the operating manual. Accepted ADRs explain decisions; proposed ADRs are not authorization to implement financial or security policies. Phase plans own future scope; status owns evidence of delivery. Current-state documentation describes code, not aspirations.

The root developers_plan.md and FRONTEND_PLAN.md, and old src/docs paths, are forwarding entry points. Update the canonical document under docs instead of recreating duplicate content.

When changing behavior, update its domain document and relevant ADR. When changing structure, update current architecture. Record verification date, command, scope and limitations before advancing a status. Preserve old audits with supersession links rather than rewriting their historical findings.

Generated documentation requires a human edit before merge, as specified in [AGENTS.md](../AGENTS.md).
