FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/InvestmentHub.Web.Client/InvestmentHub.Web.Client.csproj", "src/InvestmentHub.Web.Client/"]
COPY ["src/InvestmentHub.Contracts/InvestmentHub.Contracts.csproj", "src/InvestmentHub.Contracts/"]
RUN dotnet restore "./src/InvestmentHub.Web.Client/InvestmentHub.Web.Client.csproj"
COPY . .
WORKDIR "/src/src/InvestmentHub.Web.Client"
RUN dotnet publish "./InvestmentHub.Web.Client.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=build /app/publish/wwwroot .
COPY src/InvestmentHub.Web.Client/nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
