FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/InvestmentHub.Workers/InvestmentHub.Workers.csproj", "src/InvestmentHub.Workers/"]
COPY ["src/InvestmentHub.Application/InvestmentHub.Application.csproj", "src/InvestmentHub.Application/"]
COPY ["src/InvestmentHub.Domain/InvestmentHub.Domain.csproj", "src/InvestmentHub.Domain/"]
COPY ["src/InvestmentHub.Infrastructure/InvestmentHub.Infrastructure.csproj", "src/InvestmentHub.Infrastructure/"]
COPY ["src/InvestmentHub.Contracts/InvestmentHub.Contracts.csproj", "src/InvestmentHub.Contracts/"]
RUN dotnet restore "./src/InvestmentHub.Workers/InvestmentHub.Workers.csproj"
COPY . .
WORKDIR "/src/src/InvestmentHub.Workers"
RUN dotnet build "./InvestmentHub.Workers.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./InvestmentHub.Workers.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InvestmentHub.Workers.dll"]
