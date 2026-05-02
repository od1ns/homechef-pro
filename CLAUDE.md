# CLAUDE.md — Instrucciones para asistencia automatizada

Este archivo contiene preferencias y convenciones de quien mantiene el
proyecto, para que cualquier asistente que colabore las respete.

## Idioma de comunicación

**Hablar siempre en español neutro / latinoamericano**, no en argentino.

Diferencias clave a evitar (forma argentina → forma neutra):

| Argentino | Neutro |
|---|---|
| vos tenés | tú tienes |
| querés / podés / decís | quieres / puedes / dices |
| andá / fijate / probá | ve / fíjate / prueba |
| abrí / subí / hacé | abre / sube / haz |
| dale / che | bien / ok (omitir "che") |
| acá / allá | aquí / allá |

**Importante**: en argentino "abrí" es imperativo (forma vos) y significa
"abre tú". En español neutro "abrí" es pasado de la primera persona
("yo abrí"). Usar siempre el imperativo neutro: "abre", "abran", "abra".

Aplicable a:
- Mensajes de chat / explicaciones
- Comentarios de código (`// comentario`, `/* */`, `--`, `#`)
- Mensajes de commit (`git commit -m "..."`)
- Strings de UI en español (en Flutter, en views, etc.)
- Documentación (README, docs/)

## Stack del proyecto

Para referencia de cualquier asistente que entre por primera vez al repo:

- **Backend**: .NET 10 con Clean Architecture
  (Domain / Application / Infrastructure / Api).
- **Base de datos**: PostgreSQL 16, schema en SQL puro
  (no se usa EF Migrations — solo `EnsureCreated` ad-hoc para Identity).
- **Cache**: Redis 7.
- **Frontend**: tres apps Flutter compartiendo el paquete `shared/`:
  - `admin_web` (chef, navegador) en puerto 8090
  - `client_app` (móvil cliente, también web para demo) en puerto 8091
  - `kitchen_tablet` (tablet de cocina) en puerto 8092
- **Auth**: ASP.NET Identity + JWT Bearer + refresh tokens.
- **Pagos**: verificación manual con comprobante de imagen
  (Pago Móvil VES, Zelle, transferencia).
- **Facturación**: SENIAT/IGTF, mock provider en dev.
- **Deploy**: Docker Compose (postgres + redis + api + nginx + certbot).

## Tooling de regresión

- `scripts/smoke.ps1` — login admin + cliente, lista de menú.
- `scripts/seed-purchases.ps1` — registra compras para todos los
  ingredientes activos para que los reportes muestren costos reales.
- `scripts/smoke-deep.ps1` — flujo end-to-end completo: orden, pago,
  cocina, entrega, factura, reportes (10 pasos).
- `scripts/init-letsencrypt.sh` — emisión inicial de certificado HTTPS.

## Credenciales de desarrollo

Definidas en `deploy/.env` (excluido del repo por `.gitignore`):

- Admin: `admin@homechef.local` / `demo1234` (Bootstrap automático).
- Cliente registrado por smoke: `maria@example.com` / `demo1234`.

## Convenciones SQL

- Todo `snake_case` (tablas, columnas, vistas, funciones).
- Las 7 tablas de ASP.NET Identity también están en snake_case
  (`asp_net_users`, `asp_net_roles`, etc.) — ver
  `src/database/schema/01a_identity_tables.sql`.
- Vistas analíticas en `10_views.sql`, triggers en `11_functions_triggers.sql`.
- El cast a `numeric(N,M)` en raw SQL es obligatorio en columnas que se
  proyectan a `System.Decimal` en C#, porque algunos cálculos
  (price/cost, ratios) generan numerics con > 28 dígitos significativos
  que rompen el cast con `OverflowException`.
