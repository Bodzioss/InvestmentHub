# 0003. Use a Hybrid Persistence Strategy (Event Sourcing + CRUD)

* Status: accepted
* Date: 2026-05-07

## Context and Problem Statement

For the persistence layer of InvestmentHub, we needed to decide how to store state. A uniform approach (using CRUD everywhere, or Event Sourcing everywhere) simplifies the mental model, but often leads to using the wrong tool for the job.

For the core domain (Portfolio Management and Transaction tracking), when a user buys or sells an asset, simply overwriting the balance (CRUD) leads to the loss of historical intent. It makes answering questions like "What did my portfolio look like exactly 3 months ago?" extremely difficult. We require strict auditability and historical reconstruction for financial data.

However, for supporting domains (like User Profiles or Market Data catalogs), preserving every single state change as an event is unnecessary overhead. The history of a user changing their display name or the daily update of a stock ticker name doesn't require complex event streams.

We needed a strategy that provides strict auditability where required, without overcomplicating domains where simple state storage is sufficient.

## Decision Drivers

* **Auditability & Compliance**: Need for a strict, immutable audit trail of financial transactions (buys, sells).
* **Time Travel**: The ability to reconstruct the state of a portfolio at any given point in the past.
* **Simplicity where appropriate**: Avoid paying the "tax" of Event Sourcing complexity for simple domains.
* **CQRS Support**: The ability to project complex event streams into queryable read models for the frontend.
* **Infrastructure Simplicity**: Utilizing our existing PostgreSQL database without introducing multiple new database engines.

## Considered Options

* **Uniform CRUD**: Using Entity Framework Core or standard document storage everywhere. 
  * *Cons:* Very hard to implement robust audit trails and time travel for financial portfolios.
* **Uniform Event Sourcing**: Using Event Sourcing for everything in the system. 
  * *Cons:* Massive overkill for simple settings, resulting in unnecessary complexity and boilerplate.
* **Hybrid Approach**: Using Event Sourcing selectively for core domains, and standard CRUD for supporting domains.

## Decision Outcome

Chosen option: **Hybrid Approach using Marten for Event Sourcing in Core Domains, and standard CRUD (Marten Document DB / EF Core) for Supporting Domains**.

* **Portfolio Module**: Uses Marten's Event Sourcing capabilities to store business events (e.g., `AssetBought`, `AssetSold`). The current state is a projection of these events.
* **User/Market Data Modules**: Uses simple CRUD (e.g., Marten as a Document DB or standard relational tables) because the history of these entities is not critical to the core business logic.

### Positive Consequences

* **Right Tool for the Job**: We gain the massive benefits of Event Sourcing (Auditability, Time Travel) exactly where it matters most (financial transactions).
* **Reduced Overhead**: We do not overengineer simple features. A user profile update remains a simple `UPDATE` operation.
* **Infrastructure Synergy**: Marten (running on PostgreSQL) is uniquely positioned here. It allows us to perform *both* robust Event Sourcing and standard Document CRUD within the exact same database and library, minimizing infrastructure sprawl.

### Negative Consequences

* **Increased System Complexity**: Having two fundamentally different data access paradigms in one codebase increases cognitive load. Developers need to explicitly know *when* to use which approach.
* **Integration Challenges**: Querying data that spans across an Event-Sourced module and a CRUD module requires careful CQRS read-model design (e.g., we must project portfolio events into a read model that can be easily joined with the CRUD user data).
* **Steeper Learning Curve**: The team must understand both paradigms, how they interact, and the strict boundaries dictating their usage.
