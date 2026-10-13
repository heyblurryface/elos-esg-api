# =============================================================================
# Elos ESG API (Kaizen) - Dockerfile multi-stage
#   restore -> build -> test -> publish -> runtime
# A imagem final tem só o runtime do ASP.NET (sem SDK) e roda com usuário
# sem privilégios.
# =============================================================================

# ---------- 1) restore: camada de dependências (cache) ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src
COPY Kaizen.sln ./
COPY src/Kaizen.Api/Kaizen.Api.csproj src/Kaizen.Api/
COPY tests/Kaizen.Api.Tests/Kaizen.Api.Tests.csproj tests/Kaizen.Api.Tests/
RUN dotnet restore Kaizen.sln

# ---------- 2) build ----------
FROM restore AS build
COPY src/ src/
COPY tests/ tests/
RUN dotnet build Kaizen.sln -c Release --no-restore

# ---------- 3) test (opcional: docker build --target test .) ----------
FROM build AS test
RUN dotnet test Kaizen.sln -c Release --no-build --logger "console;verbosity=normal"

# ---------- 4) publish ----------
FROM build AS publish
RUN dotnet publish src/Kaizen.Api/Kaizen.Api.csproj -c Release --no-build -o /app/publish /p:UseAppHost=false

# ---------- 5) runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# curl só para o HEALTHCHECK
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=publish /app/publish .

# Versão da imagem (o pipeline passa o SHA do commit) exposta no /health
ARG APP_VERSION=local
ENV APP_VERSION=${APP_VERSION} \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

LABEL org.opencontainers.image.title="elos-esg-api" \
      org.opencontainers.image.description="Elos ESG - API de diagnostico de maturidade ESG (base Kaizen API)" \
      org.opencontainers.image.authors="Gabriel Bueno Barba" \
      org.opencontainers.image.version=${APP_VERSION}

EXPOSE 8080

# Usuário não-root que já vem na imagem oficial do .NET 8
USER app

HEALTHCHECK --interval=30s --timeout=5s --start-period=120s --retries=5 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Kaizen.Api.dll"]
