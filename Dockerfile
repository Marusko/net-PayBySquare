# syntax=docker/dockerfile:1

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (cached unless project files change).
COPY global.json ./
COPY PayBySquare.sln ./
COPY src/PayBySquare.Core/PayBySquare.Core.csproj src/PayBySquare.Core/
COPY src/PayBySquare.Api/PayBySquare.Api.csproj src/PayBySquare.Api/
RUN dotnet restore src/PayBySquare.Api/PayBySquare.Api.csproj

# Build & publish.
COPY src/ src/
RUN dotnet publish src/PayBySquare.Api/PayBySquare.Api.csproj -c Release -o /app /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is not in the base image; it is needed for the container health check.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

# Listen on 8080 (non-root friendly) and run as the built-in non-root user.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=0
EXPOSE 8080
USER $APP_UID

# Liveness probe against the /health endpoint.
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "PayBySquare.Api.dll"]
