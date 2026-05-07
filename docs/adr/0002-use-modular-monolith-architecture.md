# 0002. Use Modular Monolith Architecture

* Status: accepted
* Date: 2026-05-07

## Context and Problem Statement

For the InvestmentHub backend, we needed to choose an overarching architectural style. The system handles multiple distinct business areas, such as Portfolio Management, Market Data integration, and potentially User Management. We needed to decide how to structure the codebase to support future growth, maintainability, and clear logical separation, without introducing overwhelming operational complexity too early in the project lifecycle.

## Decision Drivers

* Need for strong logical boundaries between different business contexts (Bounded Contexts).
* Desire to avoid premature optimization and the high operational overhead of distributed systems.
* Ease of development and deployment (deploying a single unit is much easier than orchestrating multiple services).
* Flexibility to extract specific modules into separate microservices in the future if required by scale or organizational needs.

## Considered Options

* **Traditional Monolith (No strict structure / "Big Ball of Mud")**: A single deployable unit where code is heavily coupled and internal boundaries are not strictly enforced.
* **Modular Monolith**: A single deployable unit where the codebase is strictly organized into independent, decoupled modules (Bounded Contexts) that communicate via explicit interfaces or in-process messaging (e.g., MediatR).
* **Microservices Architecture**: Multiple independent deployable units that communicate over the network (e.g., HTTP APIs, gRPC, RabbitMQ).

## Decision Outcome

Chosen option: **Modular Monolith**, because it provides the best balance between structural discipline and operational simplicity for a project developed by a single person or small team.

### Positive Consequences

* **Low operational complexity**: We only need to build, deploy, and monitor a single backend application. We don't have to deal with distributed tracing, network latency between services, or complex orchestration (like Kubernetes) right away.
* **High maintainability**: Code is well-organized. The strict boundaries ensure that modifying the "Portfolio" module won't inadvertently break the "Market Data" module.
* **Refactoring safety**: Refactoring across modules is easier since it's a single codebase. The C# compiler will catch breaking changes immediately.
* **Future-proof**: Because the boundaries are explicitly defined, if a specific module needs to scale independently in the future, extracting it into its own microservice will be relatively straightforward.

### Negative Consequences

* **Deployment coupling**: All modules are deployed together. A critical bug or memory leak in one module can crash the entire application.
* **Technology lock-in**: All modules must share the same underlying technology stack (.NET 9). We cannot easily write one module in Python and another in C#.
* **Requires discipline**: Without the physical boundaries of a network, it is easy to accidentally violate module boundaries (e.g., a developer might be tempted to query another module's database tables directly). This requires strict code reviews or automated architectural tests (e.g., using NetArchTest).
