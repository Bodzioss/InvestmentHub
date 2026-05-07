# 0004. In-process Communication with MediatR

* Status: accepted
* Date: 2026-05-07

## Context and Problem Statement

In our Modular Monolith architecture, we need a clean, consistent way to dispatch commands (writes) and queries (reads) from the API layer to the Application layer. Furthermore, we need a way for different modules to communicate with each other in-process without becoming tightly coupled. Directly injecting Application-level services (e.g., `IPortfolioService`) into API Controllers leads to "fat controllers," tight coupling, and difficulty in managing cross-cutting concerns (like validation or logging). We need a standardized mediator pattern to decouple the "what to do" from the "how to do it."

## Decision Drivers

* **Logical CQRS**: The need to implement the Command Query Responsibility Segregation (CQRS) pattern at the logical level.
* **Thin Controllers**: Desire to keep the API layer exceptionally thin, where controllers only map HTTP requests to internal models and return HTTP responses.
* **Decoupling**: The need to decouple modules so they don't reference each other's implementation details directly.
* **Cross-Cutting Concerns**: The requirement for a middleware pipeline to seamlessly handle validation, logging, and authorization universally for all use cases.

## Considered Options

* **Direct Service Injection**: Injecting specific `I*Service` interfaces directly into API Controllers and other services.
* **Custom Mediator Implementation**: Writing our own in-house in-memory message bus / mediator.
* **MediatR Library**: Using the industry-standard `MediatR` library for the .NET ecosystem.

## Decision Outcome

Chosen option: **MediatR Library**, because it perfectly aligns with our CQRS requirements, provides an elegant pipeline behavior system, and is the standard, well-documented approach in modern .NET development.

### Positive Consequences

* **Strict CQRS Enforcement**: It forces us to define explicit `IRequest<TResponse>` (Commands/Queries) and `IRequestHandler<TRequest, TResponse>` (Handlers), physically separating the intent of an action from its execution.
* **Thin API Layer**: Controllers simply map HTTP requests to MediatR requests and call `_mediator.Send()`. They contain zero business logic.
* **Pipeline Behaviors**: We can easily add `IPipelineBehavior` implementations to handle Validation (e.g., via FluentValidation) and Logging in a single place without polluting the actual business logic handlers.
* **Loose Coupling**: Modules do not need to know about each other's handlers. If Module A needs data from Module B, it only needs access to the Query contract, not the implementation of the Handler.

### Negative Consequences

* **Indirection / "Magic"**: Developers new to MediatR might find it slightly difficult to navigate the codebase because clicking "Go to Implementation" on `_mediator.Send()` doesn't immediately jump to the handler in all IDEs. It introduces a level of indirection.
* **Performance Overhead**: While negligible for most business applications, routing every request through a mediator does add a tiny bit of reflection and allocation overhead compared to a direct method call.
* **Risk of Misuse**: It can be tempting to use MediatR for *everything*, including publishing raw Domain Events, which can sometimes blur the boundaries between Application Layer orchestration and Domain Layer logic. We must establish rules on where and how `IMediator` is used.
