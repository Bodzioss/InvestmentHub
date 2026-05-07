# 0001. Use Architecture Decision Records (ADRs) to document decisions

* Status: accepted
* Date: 2026-05-07

## Context and Problem Statement

As the InvestmentHub project evolves and transitions to a more structured engineering approach, there is a need to document key architectural and technological decisions. Without recording the history of decisions, future maintainers (or ourselves after a long break) will lack the context of *why* certain technologies or patterns were chosen (e.g., why Marten was used instead of EF Core for specific modules).

## Decision Drivers

* The need to preserve the historical context of decisions.
* Preventing repetitive discussions about the same topics.
* Improving the onboarding process.
* Tracking the project's evolution for portfolio purposes.

## Considered Options

* Architecture Decision Records (ADRs) kept in the code repository.
* External wiki tools (e.g., Notion, Confluence).
* No formal documentation (keeping knowledge in the head / "vibe coding").

## Decision Outcome

Chosen option: **Architecture Decision Records (ADRs) kept in the code repository**, because they are lightweight, versioned alongside the source code, and easy to review via GitHub Pull Requests.

### Positive Consequences

* The history of decisions lives close to the code.
* Changes can be reviewed via Pull Requests.
* The simple Markdown format requires no additional tools.

### Negative Consequences

* Requires discipline to write an ADR for every major change in the project.
