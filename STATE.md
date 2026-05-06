# HomeChef Pro — Estado del proyecto

**Ultima sesion:** 2026-05-05
**Branch principal:** `main` (todo pusheado a `github.com:od1ns/homechef-pro.git`)

Este archivo es el punto de entrada para retomar el proyecto sin
re-leer el historial completo de chat. Resume **donde estamos**,
**que hicimos**, **que falta** y **como continuar**.

---

## 1. Estado de un vistazo

| Capa | Estado |
|---|---|
| **Backend .NET 10** | Listo para deploy. 64/64 integration tests verde. |
| **Frontend admin_web (Flutter)** | Funcional con 2FA TOTP integrado. |
| **Frontend client_app (Flutter)** | Funcional. Sin 2FA (no aplica para clientes). |
| **Frontend kitchen_tablet (Flutter)** | Funcional. |
| **Schema Postgres** | Multi-tenant ready (chef_id en 21 tablas, default piloto). |
| **Auth** | JWT + refresh rotation + claim chef_id + 2FA TOTP opcional. |
| **Staging local** | Validado end-to-end con prod-mode (`docker-compose.staging.yml`). |
| **Deploy real** | Pendiente — runbook listo en `docs/audits/audit-2026-05-04-D.md` seccion 6. |

---

## 2. Audits realizados

Todos en `docs/audits/`. Cada audit es un .md autocontenido.

| Pasada | Foco | Hallazgos | Estado |
|---|---|---|---|
| A (`audit-2026-05-03-A.md`) | Auth, JWT, secretos, BFLA admin | 4 Criticals + 7 Highs | Criticals cerrados; Highs casi todos cerrados via Tier 1+2 |
| B (`audit-2026-05-04-B.md`) | Uploads, payment proof, BOLA/BOPLA orders | 4 Criticals | Cerrados con tests de regresion |
| C (`audit-2026-05-04-C.md`) | Multi-tenant readiness | 5 Bloqueantes + 4 Aplazables + 3 Operacionales | Bloqueantes cerrados via Fase 1C-A. Aplazables a Fase 2 |
| D (`audit-2026-05-04-D.md`) | Cierre pre-deploy | 0 Criticals + 5 Mediums + 4 Lows | Mediums son operacionales (runbook); Lows aceptados |

---

## 3. Findings cerrados

### Pasada A (auth/JWT/secretos)
- **F-01** docker env hardening
- **F-02** /uploads sin auth -> endpoint autenticado con regex strict + path traversal defense
- **F-03** Jwt:SigningKey con placeholder -> rechazado al startup via IValidateOptions
- **F-04** rate limiting global por IP (Tier 1)
- **F-05** security headers + HSTS + HTTPS redirect en non-dev
- **F-06** appsettings password de dev rechazado en prod
- **F-07** bootstrap admin password endurecido (rechaza placeholders comunes + min length 12)
- **F-09** magic-byte validation en uploads (Tier 2)
- **F-11** access token TTL 60 -> 15 min
- **F-12** /health/db deja de filtrar counts del negocio
- **F-13** AllowedHosts no puede ser vacio o "*" en prod
- **F-15** timing equalization en login (hash dummy si user no existe)
- **F-16** logout exige autenticacion
- **F-17** MFA TOTP para admin (backend + frontend admin_web)
- **F-21** /api/auth/register ignora roles del body (BOPLA fix)
- **F-26** optimistic concurrency con xmin de Postgres en Order/Payment/Ingredient
- **F-28** rate limiting per-IP particionado por endpoint group
- **F-31** limites en CreateGuestOrder (max 30 items, 50 qty/item, 200 total)

### Pasada B (uploads/payment/orders)
- **F-22** SubmitPayment AmountUsd debe matchear order.TotalUsd
- **F-23** PaymentProofUpload con id (no URL libre); reuse imposible
- **F-24** Order ownership via access_token (anti-IDOR)
- **F-25** SubmitPayment rechaza re-submit si order paso PendingPayment
- **F-27** validar coherencia AmountUsd/AmountPaidCurrency/ExchangeRateUsed
- **F-32** Webhook delivery sin secret -> 401

### Pasada C (multi-tenant readiness) via Fase 1C-A (6 bloques)
- **H-01** Tabla `chefs` con seed del piloto + UUID determinista
- **H-02** UNIQUE constraints compuestos `(chef_id, ...)`; correlativo per-chef
- **H-03** Issuer (RIF/LegalName/Address) en tabla `chefs`, no en appsettings
- **H-04** JWT con claim `chef_id` firmado
- **H-05** uploads/{chef_id}/folder/file en filesystem y URL

### Decisiones pendientes (riesgos aceptados o aplazados)
- **F-17 frontend client_app/kitchen_tablet** — opcional, solo admin lo usa hoy
- **H-06..H-09** (vistas SQL, roles per-chef, reports per-chef, exchange_rate per-chef) — Fase 2 cuando entre el segundo chef
- **H-10..H-12** (runbooks onboarding, backups tagged, metricas per-chef) — Fase 5/6 deploy

---

## 4. Stack tecnico actual

- **.NET 10** Clean Architecture (Domain/Application/Infrastructure/Api).
- **PostgreSQL 16** con schema en SQL puro (no EF Migrations). Aplicado via
  `src/database/schema/99_run_all.sql`.
- **Redis 7** para cache.
- **ASP.NET Identity + JWT Bearer + refresh tokens + 2FA AuthenticatorTokenProvider**.
- **MediatR + FluentValidation**.
- **Multi-tenant nivel 1**: `chef_id` en 21 tablas + JWT claim. Queries y RLS
  para Fase 2.
- **Tres apps Flutter**:
  - `admin_web` (puerto 8090) — chef
  - `client_app` (puerto 8091) — cliente movil/web
  - `kitchen_tablet` (puerto 8092) — tablet de cocina
- **Docker Compose** con dos overrides:
  - `docker-compose.prod.yml` — prod real con nginx + certbot
  - `docker-compose.staging.yml` — staging local sin nginx (acceso directo API:8080)

---

## 5. Como correr el proyecto

### Dev (single-tenant, password demo, sin HTTPS)

```powershell
cd "C:\Users\Toor\source\repos\MasterClaude\HomeChef Pro\deploy"
docker compose up -d
# API: http://localhost:8080
# Bootstrap admin: admin@homechef.local / demo1234
```

### Staging local prod-mode (validacion pre-deploy)

```powershell
cd "C:\Users\Toor\source\repos\MasterClaude\HomeChef Pro\deploy"

# Primera vez: copiar template y editar con secretos reales
cp .env.staging.local.example .env.staging.local
notepad .env.staging.local
# >>> EDITAR: reemplazar todos los REPLACE-ME-* con valores reales
#     ($jwt = openssl rand -base64 64; etc)

# Levantar stack
docker compose --env-file .env.staging.local `
    -f docker-compose.yml `
    -f docker-compose.staging.yml `
    up -d --build

# Schema de negocio
docker cp ../src/database/schema homechef-postgres:/tmp/schema
docker exec -i homechef-postgres bash -c "cd /tmp/schema && psql -U homechef -d homechef -f 99_run_all.sql"

# Smoke deep
$adminPwd = ((Get-Content .env.staging.local | Where-Object { $_ -match '^BOOTSTRAP_ADMIN_PASSWORD=' }) -split '=', 2)[1]
cd "C:\Users\Toor\source\repos\MasterClaude\HomeChef Pro"
pwsh ./scripts/smoke-deep.ps1 -ApiBase http://localhost:8080 `
    -AdminPassword $adminPwd -ClientPassword "ClienteTest1234"
```

### Frontend admin_web contra staging

```powershell
cd "C:\Users\Toor\source\repos\MasterClaude\HomeChef Pro\src\frontend\admin_web"

flutter run -d web-server --web-hostname 127.0.0.1 --web-port 8090 --release `
    --dart-define=HCP_API_BASE=http://127.0.0.1:8080
```

Browser en `http://127.0.0.1:8090` (Chrome incognito recomendado).

### Tests integration

```powershell
cd "C:\Users\Toor\source\repos\MasterClaude\HomeChef Pro\src\backend"
dotnet test tests\HomeChefPro.Api.IntegrationTests\HomeChefPro.Api.IntegrationTests.csproj `
    --filter "Category=Integration" `
    --logger "console;verbosity=minimal"
```

Esperado: 64/64 verde.

---

## 6. Pendientes en orden de impacto

### Crítico para deploy del piloto

- **Fase 6-A: deploy real al VPS.** Runbook completo en
  `docs/audits/audit-2026-05-04-D.md` seccion 6. Necesitas:
  - VPS Linux con Docker
  - Dominio (ej. `homechef.app`) con DNS apuntando al VPS
  - Email para Let's Encrypt
  Tiempo estimado: 2-3 horas con asistencia paso a paso.

- **Fase 7: onboarding del chef piloto.** Sesion sincronica:
  1. UPDATE chef piloto con sus datos reales (RIF, razon social, direccion).
  2. Cargar 5-10 ingredientes basicos.
  3. Cargar 3-5 platos del menu.
  4. Orden de prueba E2E.
  5. Activar 2FA (opcional pero recomendado).

### Importante post-piloto

- **Fase 2: refactor multi-tenant completo** cuando entre el segundo chef.
  Implica:
  - Quitar DEFAULTs de chef_id en tablas (forzar explicit).
  - EF global query filter por currentUser.ChefId.
  - Postgres RLS en cada tabla de negocio.
  - Reescribir vistas SQL con WHERE chef_id = current_setting(...).
  - Bootstrap multi-chef (admin nuevo crea su chef).
  - Roles per-chef.
  - Endpoint /api/admin/owner/dashboard cross-tenant para el dueno del SaaS.
  Tiempo estimado: 3-4 semanas.

### Nice to have (no bloquea deploy)

- F-17 frontend en client_app y kitchen_tablet (no urgente; solo admin lo usa).
- Forzar 2FA para Admin como hard requirement (hoy es opcional).
- Migrar E2E specs 04 y 05 a flutter drive + integration_test.
- CSP header en API (no critico para API-only).
- Serilog.Sinks.File para audit trail con rotacion.

---

## 7. Comandos utiles del dia a dia

```powershell
# Status del git
git status
git log --oneline -10

# Tests verdes (debe responder 64/64)
cd "C:\Users\Toor\source\repos\MasterClaude\HomeChef Pro\src\backend"
dotnet test tests\HomeChefPro.Api.IntegrationTests\HomeChefPro.Api.IntegrationTests.csproj `
    --filter "Category=Integration" --logger "console;verbosity=minimal" 2>&1 | Select-Object -Last 4

# Schema reset (dev)
docker exec -it homechef-postgres psql -U homechef -d homechef -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"
docker restart homechef-api
Start-Sleep -Seconds 8
docker cp src/database/schema homechef-postgres:/tmp/schema
docker exec -i homechef-postgres bash -c "cd /tmp/schema && psql -U homechef -d homechef -f 99_run_all.sql"

# Logs API
docker logs homechef-api --tail 30 -f

# Inspeccionar DB
docker exec -it homechef-postgres psql -U homechef -d homechef
# Adentro: \dt para ver tablas, \d+ orders para schema de orders, etc.
```

---

## 8. Convenciones del proyecto

- **Idioma de comentarios y commits**: español neutro (no argentino).
  Ver `CLAUDE.md` para tabla detallada.
- **Schema SQL**: snake_case en todas las tablas, columnas, vistas, funciones.
  Identity tables tambien (asp_net_users, asp_net_roles, etc.).
- **Tests integration**: Testcontainers Postgres real, no mocks. Cada test
  fixture aplica el schema completo desde `src/database/schema/`.
- **Multi-tenant default**: todas las tablas de negocio tienen `chef_id`
  con DEFAULT al UUID del piloto `00000000-0000-0000-0000-000000000001`.
  En Fase 2 se quitan los DEFAULTs.

---

## 9. Documentos clave

- `CLAUDE.md` — instrucciones de idioma, stack, convenciones.
- `docs/audits/audit-2026-05-03-A.md` — Pasada A (auth/JWT/secretos).
- `docs/audits/audit-2026-05-04-B.md` — Pasada B (uploads/payment).
- `docs/audits/audit-2026-05-04-C.md` — Pasada C (multi-tenant).
- `docs/audits/audit-2026-05-04-D.md` — Pasada D (cierre pre-deploy + runbook).
- `docs/runbooks/onboarding-chef.md` — pendiente, escribir en Fase 5/6.

---

## 10. Como pedirme ayuda en una proxima sesion

Cuando arranques una sesion nueva, deciles:

> Retomemos HomeChef Pro. Lee `STATE.md` para ver donde quedamos.
> Quiero hacer X.

donde X es alguna de las opciones de seccion 6.

Yo voy a leer este archivo + los audits relevantes y arrancamos.

---

**Ultima validacion runtime:** 2026-05-05 staging local
- 64/64 integration tests verde
- smoke-deep.ps1 E2E OK con order HC-20260505-0001 entregada y facturada
- 2FA TOTP setup + verify + login con codigo verificado en Chrome incognito
  contra container `homechef-api` corriendo con `ASPNETCORE_ENVIRONMENT=Production`.
