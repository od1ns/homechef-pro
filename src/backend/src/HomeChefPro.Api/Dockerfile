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

# Instalar libgssapi-krb5-2: el runtime de .NET la carga al inicio (Kerberos)
# y emite un warning si no la encuentra. No la usamos para auth pero conviene
# tenerla para silenciar el log y dejar abierta la puerta a SSPI/Kerberos.
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# Usuario no-root: las imagenes oficiales de .NET 8+ ya incluyen el usuario
# `app` (UID 1654) expuesto en la variable $APP_UID. No hay que crearlo.
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

# Pre-crear /app/uploads con ownership del user no-root, sino el primer
# Directory.CreateDirectory() en Program.cs falla con UnauthorizedAccess.
# En produccion, ademas montar un volumen sobre /app/uploads para persistencia.
RUN mkdir -p /app/uploads && chown -R $APP_UID:$APP_UID /app/uploads
VOLUME ["/app/uploads"]

USER $APP_UID

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

# El runtime base de .NET 10 es minimal (sin wget/curl). Si quieres healthcheck
# interno, mejor usar un .NET worker chico; por ahora lo cubre /health desde
# nginx + el endpoint /api/auth/me en CI.

ENTRYPOINT ["dotnet", "HomeChefPro.Api.dll"]
