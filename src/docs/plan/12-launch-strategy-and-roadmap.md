# Promissio — Launch Strategy and Long-Term Roadmap

> **Companion:** опционально к `00-core.md`. Подгружается, когда обсуждается build-in-public, контент-стратегия или Year 2 / Year 3 направления.

---

## 10. Public Launch Strategy

### Build in public

Throughout development, share progress publicly:

- Weekly LinkedIn posts summarizing the week's progress.
- Monthly blog posts diving into specific topics (day-count conventions, MCP server design, evaluation patterns).
- Twitter/Bluesky thread for every meaningful milestone.

This creates compounding visibility, generates inbound interest, and serves as a commitment device against abandonment.

### Target communities

- **r/dotnet** — primary .NET community.
- **r/csharp** — for code-quality and idiom discussions.
- **Hacker News** — for architecture and AI engineering posts.
- **Latent Space Discord** — for AI engineering visibility.
- **DotNet Kyiv** — local talks.
- **NDC, Build Stuff, JOnTheBeach** — European conference talks.
- **AI Engineer Summit / World's Fair** — AI engineering specifically.

### Talk topics derived from the project

- "Building production AI agents in regulated industries: a .NET banking case study."
- "MCP servers for enterprise systems: lessons from building Promissio."
- "Beyond annuity: implementing real loan servicing in .NET."
- "Evaluating AI agents under compliance constraints."
- "Event sourcing for audit trails: practical patterns."

---

## 11. Long-Term Roadmap

The 22-week plan delivers the core platform. Beyond that, potential expansions include:

### Year 2 — Depth

- Multi-currency support with proper FX handling.
- Securitization-friendly cash flow modeling.
- More sophisticated IFRS 9 implementation including macroeconomic overlays.
- Real-time scoring service with model versioning.
- Document analysis agent for income proofs and identity documents.
- Open Banking integration (mock initially) for cash flow-based underwriting.

### Year 2 — Breadth

- Frontend developer console using Blazor or React for demos.
- Multi-language MCP clients (Python, TypeScript examples).
- Deployment recipes for Azure and AWS.
- Helm charts for Kubernetes deployment.

### Year 3 — Ecosystem

- Cookbook of recipes for common lending scenarios.
- Tutorials for specific compliance regimes.
- Partner integrations with notable .NET libraries.
- Conference workshop materials.

---

## Appendix A — Reference Materials

### Banking domain

- ISDA documentation on day-count conventions.
- EU Consumer Credit Directive 2008/48/EC and 2023/2225 (revised).
- IFRS 9 Financial Instruments standard.
- Basel III framework documents.
- European Banking Authority guidelines on credit risk management.

### Software engineering

- "Designing Data-Intensive Applications" by Martin Kleppmann.
- "Domain-Driven Design" by Eric Evans.
- "Implementing Domain-Driven Design" by Vaughn Vernon.
- Marten documentation.
- NodaTime user guide.

### AI engineering

- "AI Engineering" by Chip Huyen.
- Anthropic engineering blog.
- Latent Space podcast and newsletter.
- Hamel Husain's writing on evaluations.
- Eugene Yan's writing on applied LLM systems.
- Model Context Protocol specification.
