# StudioB2B Multi-Tenant OMS
# Multi-stage Dockerfile

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY StudioB2B.sln .
COPY StudioB2B.Domain/StudioB2B.Domain.csproj StudioB2B.Domain/
COPY StudioB2B.Application/StudioB2B.Application.csproj StudioB2B.Application/
COPY StudioB2B.Infrastructure/StudioB2B.Infrastructure.csproj StudioB2B.Infrastructure/
COPY StudioB2B.Shared/StudioB2B.Shared.csproj StudioB2B.Shared/
COPY StudioB2B.Web/StudioB2B.Web.csproj StudioB2B.Web/

# Restore dependencies
RUN dotnet restore StudioB2B.sln

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish StudioB2B.Web/StudioB2B.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Environment variables (can be overridden at runtime)
# Production by default
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Database connection string (REQUIRED - pass at runtime)
# Example: -e ConnectionStrings__MasterDb="Server=mysql;Database=StudioB2B_Master;User=root;Password=secret;"
ENV ConnectionStrings__MasterDb=""

# Multi-tenancy settings
ENV MultiTenancy__MasterDomain="studiob2b.com"
ENV MultiTenancy__DefaultSubdomain="demo"

# Database migration settings (disabled for production by default)
ENV Database__ApplyMigrationsOnStartup=false

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

EXPOSE 8080

ENTRYPOINT ["dotnet", "StudioB2B.Web.dll"]
