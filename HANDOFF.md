# HANDOFF — HomeChef Pro (SaaS para negocios de comida)

> Respaldo preparado el 9 de noviembre de 2026. Estado técnico detallado en `STATE.md`; reglas para asistentes en `CLAUDE.md`.

| Dato | Valor |
|---|---|
| Repositorio | `git@github.com:od1ns/homechef-pro.git` — **cuenta de GitHub `od1ns`** (distinta de `od1n`) |
| Rama | `main` |
| Último commit | `1ca5c7a` (9 nov 2026) — Redis con contraseña obligatoria en staging y ajustes de modelos (hecho en este respaldo, **falta el push**) |
| Carpeta local | `D:\Desarrollo\HomeChef Pro` |
| Última sesión de desarrollo | 11 de mayo de 2026 |

## 1. Qué es

SaaS multi-tenant para cocineros/negocios de comida a domicilio: app del cliente (pedidos, modificadores de receta, pedidos programados, notificaciones push), panel administrativo web y tableta de cocina.

## 2. Tecnología

- Backend .NET 10 + PostgreSQL (multi-tenant), JWT con renovación, 2FA TOTP, códigos de invitación; 69 pruebas de integración en verde (Testcontainers).
- Frontends Flutter: `src\frontend\client_app`, `admin_web`, `kitchen_tablet`, `shared`.
- Despliegue: `deploy\docker-compose.staging.yml` (y prod), túnel de Cloudflare (temporal).
- Firebase Cloud Messaging (Etapa 5).

## 3. Documentos

`STATE.md` (punto de entrada), `docs\API.md`, `docs\DEPLOY.md`, `docs\SMOKE.md`, `docs\HANDOFF-2026-05-08.md`, `docs\HANDOFF-2026-05-09.md`, `docs\audits\`, `docs\premortems\`, `design_handoff_homechef_pro\`.

## 4. Push (cuenta distinta)

El remoto usa SSH con la cuenta `od1ns`. El push requiere la llave SSH de esa cuenta configurada en `C:\Users\Toor\.ssh\config`:

```powershell
Set-Location 'D:\Desarrollo\HomeChef Pro'
git -C 'D:\Desarrollo\HomeChef Pro' push origin main
```

## 5. Estado y pendientes

- App del cliente en Google Play (prueba interna, `app.homecheff.client`).
- Pendiente: túnel de Cloudflare con nombre y dominio propio (hoy URL temporal), siguiente etapa del frente de despliegue del piloto (ver `STATE.md` §4).
