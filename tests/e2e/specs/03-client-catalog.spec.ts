import { test, expect } from '@playwright/test';
import { URLS } from '../helpers/config';

/**
 * Verifica que el cliente puede ver el catalogo. Skipeado por la misma razon
 * que admin-login: Flutter web/CanvasKit no expone DOM real para Playwright.
 * Cubierto via API en scripts/smoke-deep.ps1 y por inspeccion visual.
 */
test.describe.skip('Client catalog', () => {
  test('Cliente anonimo navega el onboarding y ve el catalogo', async ({ page }) => {
    await page.goto(URLS.client);
    // Esperamos que la app cargue (boton "Empezar" del onboarding o ya el catalogo).
    await expect(page.getByText(/empezar|comenzar|menu de hoy|descubrir/i)).toBeVisible({
      timeout: 60000,
    });

    // Onboarding: paso 1 (bienvenida) tiene boton "Empezar".
    const start = page.getByRole('button', { name: /empezar|comenzar|start/i });
    if (await start.isVisible({ timeout: 10000 }).catch(() => false)) {
      await start.click();
      // Pasos siguientes: location, preferencias, loyalty. En cada uno hay
      // un boton "Saltar" para skipear permisos/configuracion.
      for (let i = 0; i < 5; i++) {
        const skip = page.getByRole('button', { name: /saltar|skip|continuar/i });
        if (await skip.isVisible({ timeout: 5000 }).catch(() => false)) {
          await skip.click();
        } else {
          break;
        }
      }
    }

    // Catalogo: deberian verse al menos los 3 platos seedeados.
    await expect(page.getByText(/pabell[oó]n|arepa|pasticho/i).first()).toBeVisible({
      timeout: 15000,
    });
  });
});
