/*
 * SKIPPED: Flutter web con CanvasKit (default en 3.41+) pinta todo en
 * <canvas>. El truco de click en `flt-semantics-placeholder` para activar
 * el semantics tree YA NO funciona en builds recientes — Flutter cambio
 * la forma de exponer accesibilidad.
 *
 * Para reactivar estos tests hay que elegir uno de:
 *   1. Migrar las apps Flutter a `--web-renderer=html` (deprecated 3.13+)
 *      o `--wasm` (experimental).
 *   2. Reescribir los E2E con `flutter drive` + `integration_test` package
 *      en Dart en vez de Playwright.
 *   3. Visual regression con screenshots (Percy/Chromatic).
 *
 * Mientras tanto, la cobertura E2E real esta en:
 *   - Backend: integration tests xunit (37/37 verde).
 *   - Smoke flows: scripts/smoke.ps1 + smoke-deep.ps1.
 *   - Health: specs/01-health.spec.ts.
 */

import { test, expect } from '@playwright/test';
import { URLS, CREDENTIALS } from '../helpers/config';
import { bootFlutterApp, clearSession } from '../helpers/flutter';

/**
 * E2E real contra admin_web Flutter web. Activa el semantics tree para
 * poder interactuar con los widgets que normalmente CanvasKit oculta.
 */
test.describe.skip('admin_web - UI', () => {
  test.beforeEach(async ({ page }) => {
    await clearSession(page, URLS.admin);
  });

  test('Login con credenciales correctas lleva al shell', async ({ page }) => {
    await bootFlutterApp(page, URLS.admin, /HomeChef Pro/i);

    // Login screen visible
    await expect(page.getByText(/panel de administraci/i).first())
      .toBeVisible({ timeout: 30000 });

    await page.getByLabel(/email/i).first().fill(CREDENTIALS.admin.email);
    await page.getByLabel(/contrase/i).first().fill(CREDENTIALS.admin.password);
    await page.getByRole('button', { name: /entrar|ingresar|login/i }).first().click();

    // Despues del login, la sidebar muestra "Resumen" o "Inventario"
    await expect(page.getByText(/resumen|inventario/i).first())
      .toBeVisible({ timeout: 20000 });
  });

  test('Login con password incorrecta muestra error', async ({ page }) => {
    await bootFlutterApp(page, URLS.admin, /HomeChef Pro/i);

    await expect(page.getByText(/panel de administraci/i).first())
      .toBeVisible({ timeout: 30000 });

    await page.getByLabel(/email/i).first().fill(CREDENTIALS.admin.email);
    await page.getByLabel(/contrase/i).first().fill('wrong-password-xx');
    await page.getByRole('button', { name: /entrar|ingresar|login/i }).first().click();

    // El backend devuelve 401, el ApiClient lo expone como ApiException
    // y el LoginScreen muestra un Snackbar/Banner con el mensaje.
    await expect(page.getByText(/invalid|incorrect|invalido|credenciales|error/i).first())
      .toBeVisible({ timeout: 15000 });
  });
});
