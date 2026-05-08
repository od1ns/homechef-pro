# HomeChef Pro — Estado del proyecto

**Ultima sesion:** 2026-05-07 (Sesion A invitations + Sesion B quick tunnel)
**Branch principal:** `main` (todo pusheado a `github.com:od1ns/homechef-pro.git`)

Punto de entrada para retomar el proyecto sin re-leer historial de chat.

---

## 1. Estado de un vistazo

| Capa | Estado |
|---|---|
| **Backend .NET 10** | Listo para deploy. 69/69 integration tests verde. + invitation codes (Sesion A). |
| **Frontend admin_web (Flutter)** | Funcional con 2FA TOTP + UI de invitaciones. |
| **Frontend client_app (Flutter)** | Funcional con campo "Codigo de invitacion" en registro. |
| **Frontend kitchen_tablet (Flutter)** | Funcional. |
| **Schema Postgres** | Multi-tenant ready + tabla invitation_codes (Sesion A). |
| **Auth** | JWT + refresh + claim chef_id + 2FA TOTP + invitation codes. |
| **Staging local** | Validado E2E (`docker-compose.staging.yml`). |
| **API publica HTTPS** | Validada con quick tunnel Cloudflare (url temporal). Pendiente named tunnel con dominio propio. |
| **Play Store** | Pendiente Sesion C. |

---

## 2. Audits realizados

`docs/audits/audit-2026-05-{03-A,04-B,04-C,04-D}.md` — 4 pasadas, todos los Criticals + Bloqueantes cerrados.

---

## 3. Skills disponibles

`.claude/skills/<nombre>/SKILL.md` — Claude Code las carga automaticamente:

- `premortem` — premortem estructurado
- `security-audit` — auditoria OWASP
- `bash-write-large-files` — escritura grande
- `dotnet-ef-postgres-integration-tests` — Testcontainers
- `github-actions-trx-debug` — TRX corruptos

Tambien viven en repo personal `od1n/claude-skills` (cuenta GitHub od1n,
distinta de la cuenta od1ns que tiene homechef-pro). Para usar globales en
otro computador:

```powershell
git clone git@github-od1n:od1n/claude-skills.git "$env:USERPROFILE\.claude\skills"
```

(requiere setup SSH key + alias github-od1n en `~/.ssh/config` — instrucciones
en transcript de la sesion 2026-05-07).

---

## 4. Sesion A: Codigos de invitacion (cerrada)

Backend + admin_web + client_app + tests. Validado E2E en staging local:
- Admin genera codigo en sidebar "Invitaciones".
- Cliente lo usa en registro (pantalla de crear cuenta del client_app, accesible
  desde Reseñas u otros features que requieren cuenta).
- Codigo queda con status "Agotado" tras consumirse.

**Schema:** `invitation_codes` + `invitation_code_uses` (audit trail).
**Endpoints:** `POST/GET /api/admin/invitations` + `/{id}/revoke`.
**Config:** `Bootstrap:RequireInvitationCode=true` (default true) exige codigo
en register publico. En tests integration se setea false para preservar 64
tests existentes.

---

## 5. Sesion B parcial: Cloudflare Quick Tunnel

API local expuesta a internet via cloudflared con HTTPS valido.

**Comando:**
```powershell
cloudflared tunnel --url http://localhost:8080
```

**URL temporal** (cambia cada vez que reinicia tunnel):
`https://<random-words>.trycloudflare.com`

**Limitaciones:**
- URL no persistente.
- "No uptime guarantee" segun Cloudflare.
- No sirve para Play Store production (cada cambio de URL = rebuild AAB).

**Pre-requisitos del staging local:** ALLOWED_HOSTS y CORS_ORIGIN_2 deben
incluir el dominio del tunnel (editar `deploy/.env.staging.local` y recrear
container API).

---

## 6. Pendientes en orden

### Para deploy real al piloto

- **Comprar dominio (~$10/anio en Cloudflare Registrar)** — necesario para
  named tunnel persistente y para Play Store. Ej. `homecheff.app`.

- **Sesion B real (named tunnel)** ~30min con dominio comprado:
  1. `cloudflared tunnel login` (autoriza con cuenta Cloudflare).
  2. `cloudflared tunnel create homecheff-prod`.
  3. DNS record CNAME `api.homecheff.app` → `<tunnel-id>.cfargotunnel.com`.
  4. `cloudflared.yml` apuntando a `localhost:8080`.
  5. `cloudflared tunnel run homecheff-prod` (servicio Windows si querés
     persistencia entre reboots).

- **Sesion C: AAB Android + Play Store internal testing** (~3h):
  1. Generar keystore.
  2. `flutter build appbundle --release --dart-define=HCP_API_BASE=https://api.homecheff.app`
  3. Ficha de Play Console (nombre, descripcion, icono 512x512, screenshots).
  4. Upload AAB a Internal Testing track.
  5. Agregar emails de testers.

- **Onboarding chef piloto** (sesion sincronica ~1-2h):
  1. UPDATE chef piloto con datos reales (RIF, razon social, direccion).
  2. Activar 2FA en admin (con authenticator app limpia).
  3. Cargar 5-10 ingredientes + 3-5 platos del menu.
  4. Generar codigos de invitacion para clientes.
  5. Compartir link de Play Store con testers.

### Despues del piloto

- F-17 frontend en client_app y kitchen_tablet (no urgente).
- Sistema de publicidad (~25h, sesion dedicada). Backend invitations + payments
  + IA OpenAI DALL-E 3 + UI admin/client + skill empacada. Frente 2 que decidimos
  postergar a despues de validar piloto.
- Multi-tenant Fase 2 (RLS, queries con filtro chef, marketplace) cuando entre
  el segundo chef.

---

## 7. Comandos utiles

```powershell
# Status
git status
git log --oneline -10

# Tests integration (debe ser 69/69)
cd "C:\Users\Toor\source\repos\MasterClaude\HomeChef Pro\src\backend"
dotnet test tests\HomeChefPro.Api.IntegrationTests\HomeChefPro.Api.IntegrationTests.csproj `
    --filter "Category=Integration" --logger "console;verbosity=minimal" 2>&1 | Select-Object -Last 4

# Staging local (con prod-mode + invitations)
cd "C:\Users\Toor\source\repos\MasterClaude\HomeChef Pro\deploy"
docker compose --env-file .env.staging.local `
    -f docker-compose.yml `
    -f docker-compose.staging.yml `
    up -d --build api

# Schema (incluye 19_invitation_codes.sql)
docker cp ../src/database/schema homechef-postgres:/tmp/schema
docker exec -i homechef-postgres bash -c "cd /tmp/schema && psql -U homechef -d homechef -f 99_run_all.sql"

# admin_web contra API local
cd "C:\Users\Toor\source\repos\MasterClaude\HomeChef Pro\src\frontend\admin_web"
flutter run -d web-server --web-hostname 127.0.0.1 --web-port 8090 --release `
    --dart-define=HCP_API_BASE=http://127.0.0.1:8080

# client_app contra API local
cd "C:\Users\Toor\source\repos\MasterClaude\HomeChef Pro\src\frontend\client_app"
flutter run -d web-server --web-hostname 127.0.0.1 --web-port 8091 --release `
    --dart-define=HCP_API_BASE=http://127.0.0.1:8080

# Quick tunnel (URL temporal)
cloudflared tunnel --url http://localhost:8080
# IMPORTANTE: agregar la URL al ALLOWED_HOSTS y CORS antes de probar.
```

---

## 8. Convenciones del proyecto

- Idioma neutro (no argentino) — `CLAUDE.md`.
- snake_case en SQL.
- Tests: Testcontainers Postgres real, no mocks.
- Multi-tenant: chef_id con DEFAULT al UUID piloto en todas las tablas de negocio.
- Skills: `.claude/skills/<nombre>/SKILL.md` per-proyecto, o `~/.claude/skills/` globales del usuario.

---

## 9. Documentos clave

- `CLAUDE.md` — instrucciones de idioma, stack, convenciones.
- `docs/audits/audit-2026-05-03-A.md` a `audit-2026-05-04-D.md` — 4 audits.
- `STATE.md` (este archivo) — punto de entrada para retomar.

---

## 10. Como pedirme ayuda en una proxima sesion

Cuando arranques una sesion nueva:

> Retomemos HomeChef Pro. Lee `STATE.md` para ver donde quedamos.
> Quiero hacer X.

donde X puede ser:
- Comprar dominio + Sesion B real (named tunnel).
- Sesion C (AAB Android + Play Store).
- Onboarding chef piloto.
- Sistema de publicidad (Frente 2 pospuesto).
- Cualquier otra cosa.

---

**Ultima validacion runtime:** 2026-05-07
- 69/69 integration tests verde
- Sesion A (invitation codes) E2E OK contra staging local + admin_web + client_app
- Sesion B Quick Tunnel: API publica `https://<random>.trycloudflare.com/health`
  respondio 200 desde celular en datos moviles
