# 0007. Development Orchestration with .NET Aspire

* Status: accepted
* Date: 2026-05-07

## Context and Problem Statement

The InvestmentHub project relies on a distributed architecture. Even though the backend is a Modular Monolith, the complete system includes a .NET API, a Next.js frontend, a PostgreSQL database, RabbitMQ for messaging, and Redis for caching. Managing the local development environment—starting all these diverse services, ensuring they can communicate, injecting the correct connection strings, and monitoring their logs—has become cumbersome. Traditional tools like `docker-compose` handle infrastructure well but lack deep integration with the .NET application lifecycle, debugging experience, and modern observability out-of-the-box. We needed a unified way to orchestrate the local development environment to ensure a frictionless developer experience.

## Decision Drivers

* **Developer Experience (DX) / "F5 Experience"**: A developer should be able to clone the repository, press a single button (or run one command), and have the entire system (infrastructure + applications) running, connected, and ready for debugging.
* **Service Discovery & Configuration**: The need for automatic injection of connection strings and endpoint URLs between services, eliminating the need to hardcode ports or manage complex, error-prone `.env` files.
* **Local Observability**: Built-in access to structured logs, distributed traces, and metrics during local development to catch performance issues and bugs early.
* **Ecosystem Alignment**: A solution that integrates seamlessly with our primary backend technology stack (.NET 9).

## Considered Options

* **Docker Compose**: The traditional industry standard for orchestrating local infrastructure. 
  * *Pros:* Universal, language-agnostic. 
  * *Cons:* Seamless debugging of code running in containers can be tricky; connection string and port management is manual; lacks a built-in, unified observability dashboard.
* **Manual Scripts (PowerShell/Bash)**: Writing custom scripts to spin up Docker containers and `dotnet run` / `npm run dev` commands. 
  * *Pros:* Total control. 
  * *Cons:* High maintenance burden, often flaky, and generally provides a poor cross-platform experience.
* **.NET Aspire**: An opinionated, cloud-ready stack by Microsoft for building observable, production-ready, distributed applications.

## Decision Outcome

Chosen option: **.NET Aspire**, because it is purpose-built for the modern .NET ecosystem, offering an unparalleled local development experience with automatic service discovery, a built-in OpenTelemetry dashboard, and seamless integration with infrastructure dependencies.

### Positive Consequences

* **The Ultimate "F5 Experience"**: Running the `InvestmentHub.AppHost` project automatically spins up PostgreSQL, RabbitMQ, Redis (via containers), the .NET API, and even orchestrates the Node.js/Next.js frontend process.
* **Automatic Service Discovery**: Connection strings and URLs are automatically generated and injected. For example, registering PostgreSQL in the AppHost automatically provides the correct connection string to the API project without manual configuration.
* **Built-in Developer Dashboard**: Aspire provides a brilliant local web dashboard showing structured logs, environment variables, distributed traces (via OpenTelemetry), and metrics for all running services and containers out-of-the-box.
* **Infrastructure as Code (in C#)**: The local orchestration setup is written in C#, meaning it is strongly typed, easily refactorable, and uses the language the backend team already knows.

### Negative Consequences

* **Relatively New Technology**: .NET Aspire is a newer addition to the ecosystem. We might encounter edge-case bugs, breaking changes in newer versions, or less community documentation compared to highly mature tools like Docker Compose.
* **Strict Docker Dependency**: Aspire relies heavily on a local container runtime (like Docker Desktop) to spin up infrastructure components. It will not work on a machine lacking a functional container environment.
* **Microsoft Ecosystem Bias**: While Aspire can orchestrate Node.js applications, its features shine brightest when used with .NET. It adds a Microsoft-specific orchestration layer to the project.
