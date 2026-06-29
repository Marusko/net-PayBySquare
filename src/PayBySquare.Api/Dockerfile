# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

# Switch to root to install curl
USER root

# Install curl
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/PayBySquare.Api/PayBySquare.Api.csproj", "src/PayBySquare.Api/"]
COPY ["src/PayBySquare.Core/PayBySquare.Core.csproj", "src/PayBySquare.Core/"]
RUN dotnet restore "./src/PayBySquare.Api/PayBySquare.Api.csproj"
COPY . .
WORKDIR "/src/src/PayBySquare.Api"
RUN dotnet build "./PayBySquare.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PayBySquare.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final

LABEL org.opencontainers.image.title=".NET PayBySquare API" \
      org.opencontainers.image.description="PayBySquare selfhosted API service" \
      org.opencontainers.image.version="1.0.0"

      # Liveness probe against the /health endpoint.
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PayBySquare.Api.dll"]