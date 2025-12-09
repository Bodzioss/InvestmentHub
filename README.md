# InvestmentHub

> [!NOTE]
> **This application is currently under construction.**

InvestmentHub is a comprehensive investment portfolio management platform designed for individual investors. The application allows real-time asset tracking, historical performance analysis, and management of multiple portfolios simultaneously.

The system is built on a modern microservices architecture, utilizing CQRS and Event Sourcing patterns, ensuring high scalability, auditability, and performance.

## Key Features

*   **Portfolio Management**: Create and edit multiple investment portfolios.
*   **Asset Tracking**: Support for stocks, bonds, ETFs, and other financial instruments.
*   **Market Data**: Integration with external data providers (Yahoo Finance) for valuation updates.
*   **Performance Analysis**: Calculation of returns, profit/loss, and portfolio value over time.
*   **Notifications**: Real-time notification system (SignalR).

## Technologies and Tools

The project utilizes the latest technologies from the .NET stack and modern cloud-native solutions.

### Backend (.NET 9)

*   **ASP.NET Core Web API**: Main communication layer.
*   **Marten**: Library for Event Sourcing and Document DB on PostgreSQL.
*   **MassTransit**: Message Bus for asynchronous communication (RabbitMQ).
*   **MediatR**: Mediator pattern implementation for CQRS (Command Query Responsibility Segregation).
*   **FluentValidation**: Model and command validation.
*   **Hangfire**: Background job processing.
*   **SignalR**: Real-time communication with the client.
*   **YahooQuotesApi**: Stock market data retrieval.

### Frontend

*   **Blazor WebAssembly**: C#-based SPA (Single Page Application) framework.
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
*   **Azure Static Web Apps**: Frontend application hosting (Blazor).
