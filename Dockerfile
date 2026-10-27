# ==============================================================================
# EnterpriseERP.API - Production Container Image
# Multi-Stage Build optimized for .NET 9 & .NET 10 Web API
# Compatible with Render, Fly.io, Railway, Koyeb, AWS ECS, Azure Container Apps
# ==============================================================================

# STAGE 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies (layer cache optimization)
COPY ["EnterpriseERP.API.csproj", "./"]
RUN dotnet restore "EnterpriseERP.API.csproj"

# Copy source code and build release
COPY . .
RUN dotnet publish "EnterpriseERP.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# STAGE 2: Runtime Environment
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Standard cloud port binding (8080 is the default convention for .NET 8+ and Cloud PaaS)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Copy published artifacts
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "EnterpriseERP.API.dll"]
