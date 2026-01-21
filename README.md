# InvestmentHub

> [!NOTE]
> **This application is currently under active development.**

InvestmentHub is a comprehensive investment portfolio management platform designed for individual investors. The application enables real-time asset tracking, historical performance analysis, and management of multiple portfolios simultaneously.

The system is built on a modern microservices architecture, utilizing CQRS and Event Sourcing patterns, ensuring high scalability, auditability, and performance.

## Key Features

*   **Portfolio Management**: Create and edit multiple investment portfolios.
*   **Asset Tracking**: Support for stocks, bonds, ETFs, and other financial instruments.
*   **Market Data**: Integration with external data providers (Yahoo Finance) for valuation updates.
*   **Performance Analysis**: Calculation of returns, profit/loss, and portfolio value over time.
*   **Transaction History**: Track income/expense transactions with full audit trail.
*   **Notifications**: Real-time notification system (SignalR).

## Technologies and Tools

The project utilizes the latest technologies from the .NET stack and modern frontend frameworks.

### Backend (.NET 9)

*   **ASP.NET Core Web API**: Main communication layer.
*   **Marten**: Library for Event Sourcing and Document DB on PostgreSQL.
*   **MassTransit**: Message Bus for asynchronous communication (RabbitMQ).
*   **MediatR**: Mediator pattern implementation for CQRS (Command Query Responsibility Segregation).
*   **FluentValidation**: Model and command validation.
*   **Hangfire**: Background job processing.
*   **SignalR**: Real-time communication with the client.
*   **YahooQuotesApi**: Stock market data retrieval.

### Frontend (React)

*   **Next.js 16**: React framework with App Router and server components.
*   **React 19**: Latest React with concurrent features.
*   **TypeScript**: Type-safe development.
*   **TailwindCSS 4**: Utility-first CSS framework.
*   **Radix UI**: Accessible, unstyled UI primitives.
*   **shadcn/ui**: Beautifully designed components built on Radix UI.
*   **TanStack Query (React Query)**: Server state management and caching.
*   **Zustand**: Lightweight client state management.
*   **React Hook Form + Zod**: Form handling with schema validation.
*   **Recharts**: Data visualization and charting.

### Frontend (Blazor) - Legacy

*   **Blazor WebAssembly**: C#-based SPA framework.
*   **MudBlazor**: Material Design-based UI component library.

### Infrastructure & DevOps

*   **Docker**: Application containerization.
*   **PostgreSQL**: Main database (relational and JSON document data) hosted on **Neon**.
*   **RabbitMQ**: Message broker hosted on **CloudAMQP**.
*   **Redis**: Cache hosted on **Upstash**.
*   **Seq**: Centralized logging and monitoring.
*   **OpenTelemetry**: Observability and tracing.
*   **.NET Aspire**: Local development environment orchestration.

### Cloud (Azure)

*   **Azure Container Apps**: Microservices hosting (API, Workers).
*   **Azure Static Web Apps / Vercel**: Frontend application hosting.

## Project Structure

```
├── host/                    # .NET Aspire orchestration
├── src/
│   ├── InvestmentHub.API/           # ASP.NET Core Web API
│   ├── InvestmentHub.Application/   # Application layer (use cases)
│   ├── InvestmentHub.Contracts/     # Shared DTOs and contracts
│   ├── InvestmentHub.Domain/        # Domain entities and logic
│   ├── InvestmentHub.Infrastructure/# Data access and external services
│   ├── InvestmentHub.Workers/       # Background job workers
│   ├── InvestmentHub.Web.Client/    # Blazor WebAssembly (legacy)
│   └── investment-hub-react/        # Next.js/React frontend (primary)
└── tests/                   # Unit, integration, and E2E tests
```

## Getting Started

### Prerequisites

*   .NET 9 SDK
*   Node.js 20+
*   Docker (for local infrastructure)

### Running Locally

1.  Start infrastructure with .NET Aspire:
    ```bash
    dotnet run --project host/InvestmentHub.AppHost
    ```

2.  Start React frontend:
    ```bash
    cd src/investment-hub-react
    npm install
    npm run dev
    ```
