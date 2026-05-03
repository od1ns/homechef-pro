# Tests E2E (Playwright)

Tests de integración end-to-end que ejecutan flujos reales contra las apps
Flutter web + backend, con clicks reales en navegador (Chromium headless).

## Pre-requisitos

Antes de correr los tests, asegúrate de tener todo el stack levantado:

```powershell
# 1. Backend en Docker
cd ..\..\deploy
docker compose up -d
Start-Sleep -Seconds 20

# 2. Datos seedeados (compras + cliente Maria)
cd ..
pwsh ./scripts/smoke.ps1
pwsh ./scripts/seed-purchases.ps1

# 3. Apps Flutter (cada una en su terminal)
cd src\frontend\admin_web
flutter run -d web-server --web-port=8090 --dart-define=HCP_API_BASE=http://localhost:8080

# Otra terminal:
cd src\frontend\client_app
flutter run -d web-server --web-port=8091 --dart-define=HCP_API_BASE=http://localhost:8080
```

## Setup inicial (una sola vez)

```powershell
cd tests\e2e
npm install
npm run install:browsers   # descarga Chromium para Playwright (~150 MB)
```

## Correr los tests

```powershell
# Todos los tests, headless
npm test

# Modo headed (ver el navegador)
npm run test:headed

# Modo UI interactivo (Playwright Inspector)
npm run test:ui

# Un test específico
npx playwright test specs/02-admin-login.spec.ts

# Ver el reporte HTML del último run
npm run report
```

## Estructura

```
tests/e2e/
├── package.json              # Dependencias (Playwright)
├── playwright.config.ts      # Config: workers=1, retries en CI, screenshots en fail
├── helpers/
│   ├── config.ts             # URLs y credenciales (admin/Maria con demo1234)
│   └── backend.ts            # Helpers para registrar Maria, login admin via API
└── specs/
    ├── 01-health.spec.ts     # /health, /health/db, las 2 SPAs Flutter
    ├── 02-admin-login.spec.ts # Login admin OK + login con password mala
    └── 03-client-catalog.spec.ts # Onboarding cliente → catálogo seedeado
```

## Cómo extender

Para agregar tests nuevos:

1. Crear `specs/NN-nombre-corto.spec.ts` (numerado para mantener orden).
2. Importar `URLS` y `CREDENTIALS` de `helpers/config.ts`.
3. Si necesitás datos de prueba (compras, órdenes), llamar a la API directamente
   con `request.newContext()` para no depender del UI.
4. Para selectores resilientes en Flutter web, usar `getByLabel`, `getByRole`,
   `getByText`. Evitar selectores CSS porque Flutter genera nombres aleatorios.

## Por qué workers=1

Los tests comparten estado del backend (BD compartida). Si dos tests
corrieran en paralelo y ambos crearan órdenes, tendrías colisiones de
fiscal numbers, conteos de inventario inconsistentes, etc. Para tests de
verdad paralelos hay que aislar la BD por test (Testcontainers o
docker-compose por worker), que es harina de otro costal.

## Limitación con Flutter web + CanvasKit

Flutter web 3.41 usa **CanvasKit** por defecto: pinta todo el contenido
(textos, botones, formularios) en un `<canvas>` único. No hay DOM real
con esos elementos hasta que se active el **semantics tree** (clickeando
`flt-semantics-placeholder` o vía JS).

Por eso los tests de UI con clicks en inputs o botones (ver
`02-admin-login.spec.ts` y `03-client-catalog.spec.ts`) están **skipeados
con `test.describe.skip`** hasta que implementemos uno de:

1. **Activador de semantics**: helper que ejecute en `beforeEach`:
   ```js
   page.evaluate(() => window._flutter?.semanticsEnabled = true);
   ```
   (No siempre funciona — depende de la versión de Flutter.)

2. **HTML renderer**: relanzar `flutter run` con
   `--web-renderer=html` o `--dart-define=FLUTTER_WEB_USE_SKIA=false`.
   Eso hace que Flutter use HTML real para textos, accesible por
   `getByText`. Trade-off: peor performance gráfica.

3. **Visual regression con screenshots**: en lugar de buscar elementos,
   comparar screenshots contra baselines. Más robusto pero más frágil
   ante cambios de diseño.

Mientras tanto, los tests funcionales reales están cubiertos por:
- `scripts/smoke.ps1` y `scripts/smoke-deep.ps1` (flujo end-to-end vía API)
- Inspección manual del navegador en dev

## Pendiente (mejoras)

- Activar semantics tree y desbloquear los specs de login/catalog skipeados.
- Test del flujo de polling: orden creada → admin avanza → cliente ve cambio.
- Test del refresh token (forzar 401 borrando token, ver que reauth funciona).
- Test del módulo Sabor (canjear recompensa, ver saldo restante).
- Integración con CI: agregar un workflow que levante el stack y corra los tests.
