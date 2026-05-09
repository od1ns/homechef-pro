# HomeChef Pro — Estado del proyecto

**Ultima sesion:** 2026-05-08 (Sesion C: AAB Android + Play Store internal testing)
**Branch principal:** `main` (todo pusheado a `github.com:od1ns/homechef-pro.git`)

Punto de entrada para retomar el proyecto sin re-leer historial de chat.

---

## 1. Estado de un vistazo

| Capa | Estado |
|---|---|
| **Backend .NET 10** | Listo. 69/69 integration tests verde. |
| **Frontend admin_web (Flutter)** | Funcional + 2FA TOTP + UI de invitaciones. |
| **Frontend client_app (Flutter)** | Funcional + campo invitacion + AAB Android subido a Play Store. |
| **Frontend kitchen_tablet (Flutter)** | Funcional. |
| **Schema Postgres** | Multi-tenant ready + invitation_codes. |
| **Auth** | JWT + refresh + chef_id claim + 2FA TOTP + invitation codes. |
| **Staging local** | Validado E2E (`docker-compose.staging.yml`). |
| **API publica HTTPS** | ✅ Cloudflare quick tunnel (URL temporal). Pendiente named tunnel con dominio. |
| **Play Store** | ✅ Internal Testing activo. App `app.homecheff.client` instalable desde Play Store oficial. |
| **Validacion E2E celular real** | ✅ Cliente registro con codigo + entra a la app desde Android via Play Store + tunnel. |

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

(requiere setup SSH key + alias github-od1n en `~/.ssh/config`).

---

## 4. Sesiones del Frente 1 (deploy del piloto)

### Sesion A: Codigos de invitacion ✅ CERRADA

Backend + admin_web + client_app + tests. Validado E2E.
- Schema: `invitation_codes` + `invitation_code_uses` (audit trail).
- Endpoints: `POST/GET /api/admin/invitations` + `/{id}/revoke`.
- `Bootstrap:RequireInvitationCode=true` (default) exige codigo en register.
- UI: pantalla "Invitaciones" en admin_web; campo "Codigo de invitacion" en
  registro de client_app.

### Sesion B: Cloudflare Tunnel — PARCIAL (quick tunnel)

Quick tunnel valido HTTPS, pero **URL temporal** que cambia al reiniciar.

```powershell
cloudflared tunnel --url http://localhost:8080
```

**Pendiente para produccion:** named tunnel con dominio propio. Cuando
tengas el dominio, ~30min:
1. `cloudflared tunnel login`.
2. `cloudflared tunnel create homecheff-prod`.
3. DNS CNAME `api.tudominio.com` -> `<tunnel-id>.cfargotunnel.com`.
4. `cloudflared.yml` apuntando a localhost:8080.
5. Run como servicio Windows (persistente entre reboots).

### Sesion C: AAB Android + Play Store Internal Testing ✅ CERRADA

Validado E2E desde Play Store oficial:
- Keystore generado: `C:\Users\Toor\.android-keystores\homecheff-release.jks`
- Password en password manager (NO en el repo, NO en chat).
- `key.properties` en `client_app/android/` con credentials (gitignored).
- AAB firmado con keystore release.
- Subido a Play Console -> Internal Testing track de "HomeCheff" (package
  `app.homecheff.client`).
- App descargable desde Play Store via opt-in URL.
- Registro con codigo + entrada al menu funciona desde celular.

**Limitacion del piloto actual:** la app esta hardcodeada con la URL del
quick tunnel actual. Si el tunnel se cae o reinicia, la app deja de
funcionar. Para arreglarlo definitivamente: dominio + named tunnel +
recompilar AAB con URL fija.

---

## 5. Pendientes para deploy real al chef piloto

### Para mover a Production (Play Store)

- **Comprar dominio (~$10/anio en Cloudflare Registrar)**. Sugerencia:
  `homecheff.app`.
- **Sesion B real (named tunnel)** ~30min con dominio.
- **Recompilar AAB** con URL del tunnel persistente como `HCP_API_BASE`.
- **Politica de privacidad publica** — Play Store la exige para
  produccion. Texto + Gist publico de GitHub.
- **Subir version 1.0.0+4** a Closed testing (mas testers) o Production.

### Onboarding chef piloto (sesion sincronica ~1-2h)

1. UPDATE chef piloto con datos reales (RIF, razon social, direccion).
2. Activar 2FA admin con authenticator app fresca.
3. Cargar 5-10 ingredientes + 3-5 platos del menu.
4. Generar codigos de invitacion para clientes.
5. Compartir link de Play Store con testers iniciales.

### Despues del piloto

- F-17 frontend en client_app y kitchen_tablet (no urgente).
- Sistema de publicidad (Frente 2 pospuesto, ~25h, sesion dedicada).
- Multi-tenant Fase 2 (RLS, queries con filter, marketplace) cuando entre
  el segundo chef.

---

## 6. Commits clave del proyecto

```
df2001e docs(audit): Pasada C
304a93d Fase 1C-A Bloque 1 (schema chef_id)
a7f8e50 Fase 1C-A Bloque 2 (Domain Chef + EF)
cc2e7ed Fase 1C-A Bloque 3 (JWT chef_id)
8126f1c Fase 1C-A Bloque 4 (Issuer en chefs)
7f81dc3 Fase 1C-A Bloque 5 (uploads per-tenant)
f176382 Fase 1C-A Bloque 6 (tests readiness)
6437feb F-26 (xmin concurrency)
90af750 Fase 6-B (staging local)
95d528e F-17 backend MFA TOTP
eabf8a3 F-17 frontend admin_web MFA
2fc9b04 Sesion A backend invitations
c1085e7 Sesion A UI invitations
50232b6 STATE.md cierre 2026-05-07
cf5386d Sesion C AAB Android Play Store
```

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

# Staging local
cd "C:\Users\Toor\source\repos\MasterClaude\HomeChef Pro\deploy"
docker compose --env-file .env.staging.local `
    -f docker-compose.yml -f docker-compose.staging.yml `
    up -d --build api

# Quick tunnel (URL temporal, cambia cada reinicio)
cloudflared tunnel --url http://localhost:8080
# IMPORTANTE: actualizar ALLOWED_HOSTS y CORS en .env.staging.local con la URL nueva.

# Compilar AAB Android
cd "C:\Users\Toor\source\repos\MasterClaude\HomeChef Pro\src\frontend\client_app"
flutter build appbundle --release `
    --dart-define=HCP_API_BASE=https://<tunnel-url>.trycloudflare.com
# Output: build\app\outputs\bundle\release\app-release.aab

# Bumpear versionCode antes de cada AAB nuevo (Play Store no acepta duplicados)
# pubspec.yaml: version: 0.1.0+N -> 0.1.0+(N+1)
```

---

## 8. Archivos sensibles NO versionados

- `deploy/.env.staging.local` — secrets del API en staging
- `src/frontend/client_app/android/key.properties` — password keystore
- `C:\Users\Toor\.android-keystores\homecheff-release.jks` — keystore
  (BACKUP CRITICO: si se pierde, no se puede actualizar la app en Play Store)

Backup recomendado del keystore: copiarlo a Bitwarden / 1Password / OneDrive
encriptado / cualquier storage seguro NO publico.

---

## 9. Como retomar en proxima sesion

> Retomemos HomeChef Pro. Lee `STATE.md`. Quiero hacer X.

donde X puede ser:
- "Compre dominio Y. Vamos por named tunnel persistente."
- "Sesion de onboarding chef piloto: cargar menu real."
- "Sistema de publicidad (Frente 2)."
- "Multi-tenant Fase 2 (RLS + queries con filter)."

---

**Ultima validacion runtime:** 2026-05-08
- 69/69 integration tests verde
- AAB Android instalado en celular via Play Store oficial
- Registro de cuenta con codigo de invitacion E2E OK desde celular Android
  contra API local servido via Cloudflare quick tunnel
