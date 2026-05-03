import { test, expect } from '@playwright/test';
import { URLS, CREDENTIALS } from '../helpers/config';

/**
 * Verifica el flujo de login del admin contra admin_web Flutter web.
 *
 * NOTA: Flutter web 3.41 con CanvasKit pinta todo en <canvas> y no expone
 * los inputs/botones como DOM accesible salvo que el "semantics tree" este
 * activado. Estos tests estan skipeados hasta que tengamos un setup de
 * Flutter con --web-renderer=html o un helper que active semantics via JS.
 * Mientras tanto, el flujo real de login esta cubierto por:
 *   - scripts/smoke.ps1 (login admin via API)
 *   - tests/Auth.IntegrationTests (cuando arreglemos los 21 fallos pre-existentes)
 */
test.describe.skip('Admin login', () => {
  test('Login con credenciales correctas lleva al shell', async ({ page }) => {
    await page.goto(URLS.admin);
    // Esperamos que el login screen este renderizado (texto unico).
    await expect(page.getByText(/panel de administraci/i)).toBeVisible({
      timeout: 60000,
    });

    // Buscar el input de Email (Flutter expone semantica accesible).
    const email = page.getByLabel(/email/i);
    const password = page.getByLabel(/contrase/i); // contraseña con tilde
    await email.fill(CREDENTIALS.admin.email);
    await password.fill(CREDENTIALS.admin.password);

    await page.getByRole('button', { name: /entrar/i }).click();

    // Tras login, la sidebar muestra "Resumen" como tab activa.
    await expect(page.getByText('Resumen')).toBeVisible({ timeout: 15000 });
    await expect(page.getByText(/inventario/i)).toBeVisible();
  });

  test('Login con password incorrecta muestra error', async ({ page }) => {
    await page.goto(URLS.admin);
    await expect(page.getByText(/panel de administraci/i)).toBeVisible({
      timeout: 60000,
    });

    const email = page.getByLabel(/email/i);
    const password = page.getByLabel(/contrase/i);
    await email.fill(CREDENTIALS.admin.email);
    await password.fill('wrong-password');

    await page.getByRole('button', { name: /entrar/i }).click();

    // Mensaje de error generico desde el backend; el ApiClient lo expone
    // como ApiException.message → "Invalid email or password."
    await expect(page.getByText(/invalid|incorrect|invalido/i)).toBeVisible({
      timeout: 10000,
    });
  });
});
