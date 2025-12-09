FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/InvestmentHub.API/InvestmentHub.API.csproj", "src/InvestmentHub.API/"]
COPY ["src/InvestmentHub.Application/InvestmentHub.Application.csproj", "src/InvestmentHub.Application/"]
COPY ["src/InvestmentHub.Domain/InvestmentHub.Domain.csproj", "src/InvestmentHub.Domain/"]
COPY ["src/InvestmentHub.Infrastructure/InvestmentHub.Infrastructure.csproj", "src/InvestmentHub.Infrastructure/"]
COPY ["src/InvestmentHub.Contracts/InvestmentHub.Contracts.csproj", "src/InvestmentHub.Contracts/"]
RUN dotnet restore "./src/InvestmentHub.API/InvestmentHub.API.csproj"
COPY . .
WORKDIR "/src/src/InvestmentHub.API"
RUN dotnet build "./InvestmentHub.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./InvestmentHub.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InvestmentHub.API.dll"]
