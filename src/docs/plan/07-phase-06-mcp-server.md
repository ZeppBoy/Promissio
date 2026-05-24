# Phase 6 — AI Operations Layer: MCP Server (Weeks 13–15)

> **Companion:** всегда подгружать вместе с `00-core.md`.
> **Weeks:** 13, 14, 15
> **Goal:** Expose loan operations as MCP tools consumable by any compatible AI client.

---

## Week 13 — MCP Server Foundation

### Tasks

1. Set up MCP server as a standalone process using ModelContextProtocol C# SDK.
2. Implement authentication and authorization for tool invocations.
3. Implement initial tool set:
   - `get_loan_by_id`
   - `search_loans` with filters
   - `get_payment_history`
   - `get_schedule`
   - `calculate_payoff_amount` for any future date
   - `simulate_restructuring` for what-if scenarios

### Acceptance criteria

- MCP server is connectable from Claude Desktop.
- All tools work with realistic loan data.
- Authorization prevents cross-tenant data access.

---

## Week 14 — Advanced Tools

### Tasks

1. Implement `analyze_loan_health` — generates risk score, days past due trend, payment behavior summary.
2. Implement `generate_payment_reminder` — drafts customer-facing message respecting compliance constraints.
3. Implement `propose_restructuring_options` — given customer financial situation, suggests restructuring options.

### Acceptance criteria

- All advanced tools produce structured, well-typed outputs.
- Generated messages pass a compliance check (no threats, no improper third-party disclosure).

---

## Week 15 — Documentation and Client Testing

### Tasks

1. Write comprehensive MCP server documentation in `/docs/mcp/`.
2. Manually test all tools through Claude Desktop with realistic scenarios.
3. Record demo video showing a banker workflow using Claude with Promissio MCP server.

### Acceptance criteria

- Documentation is sufficient for a third-party developer to connect and use the server.
- Demo video is published and linked from README.

---

## AI delegation notes

MCP tool design (which tools to expose, parameter schemas, security boundaries) should be the author's decision. Implementation of individual tools is well-suited to AI assistance.
