# ---------------------------------------------------------------------
# HomeChef Pro API - Dockerfile multi-stage
# ---------------------------------------------------------------------
# Build context esperado:  repo root (HomeChef Pro/)
# docker build -f src/backend/src/HomeChefPro.Api/Dockerfile -t homechef-api .
# ---------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Archivos de nivel repo-backend para aprovechar el cache
COPY src/backend/Directory.Build.props         .
COPY src/backend/Directory.Packages.props      .
COPY src/backend/HomeChefPro.slnx              ./HomeChefPro.slnx

# Copiar solo los csproj primero (mejor caching de restore)
COPY src/backend/src/HomeChefPro.Domain/*.csproj         ./src/HomeChefPro.Domain/
COPY src/backend/src/HomeChefPro.Application/*.csproj   ./src/HomeChefPro.Application/
COPY src/backend/src/HomeChefPro.Infrastructure/*.csproj ./src/HomeChefPro.Infrastructure/
COPY src/backend/src/HomeChefPro.Api/*.csproj           ./src/HomeChefPro.Api/

RUN dotnet restore ./src/HomeChefPro.Api/HomeChefPro.Api.csproj

# Copiar el resto del código
COPY src/backend/src ./src

WORKDIR /src/src/HomeChefPro.Api
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Usuario no-root
RUN adduser --disabled-password --gecos "" --uid 10001 appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

HEALTHCHECK --interval=30s --timeout=3s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "HomeChefPro.Api.dll"]
