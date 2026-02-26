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


# Copy all source code
COPY . .

# Restore and publish in one layer to avoid NuGet cache issues
RUN dotnet publish StudioB2B.Web/StudioB2B.Web.csproj \
    -c Release \
    -o /app/publish

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
ENV ASPNETCORE_URLS=http://+:5184

# Database connection string (REQUIRED - pass at runtime)
# Example: -e ConnectionStrings__MasterDb="Server=mysql;Database=StudioB2B_Master;User=root;Password=secret;"
ENV ConnectionStrings__MasterDb=""

# Multi-tenancy settings
ENV MultiTenancy__MasterDomain="studiob2b.com"

# Tenant DB connection string template (for per-tenant databases)
ENV MultiTenancy__TenantDbConnectionTemplate=""

ENV Jwt__Secret="2B5F9B6E75C256999B7359FE076156E5"

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5184/health || exit 1

EXPOSE 5184

ENTRYPOINT ["dotnet", "StudioB2B.Web.dll"]
