# Contributing to Promissio

Thank you for your interest in contributing!

## Getting Started

1. Fork the repository.
2. Clone your fork and create a feature branch.
3. Make your changes following the conventions below.
4. Open a pull request against `main`.

## Branch Naming

- Feature branches: `feat/<short-description>`
- Fix branches: `fix/<short-description>`
- Documentation branches: `docs/<short-description>`

## Commit Messages

Use Conventional Commits format:

```
<type>(<scope>): <short summary>

<longer body explaining why this change is needed>
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`, `build`, `ci`.

## Code Standards

- Run `dotnet format` before committing. CI will reject unformatted code.
- All tests must pass. Do not delete or disable tests.
- Follow the rules in [AGENTS.md](AGENTS.md) for coding conventions, domain modeling, and banking semantics.
- Read relevant ADRs before modifying a subsystem.

## Pull Requests

- One PR per logical change.
- Include description: what changed, why, how to test, ADR references.
- Keep PRs under 400 lines of production code diff.

## Questions?

Open an issue or reach out to the project maintainers.
