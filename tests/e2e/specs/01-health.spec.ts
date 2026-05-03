import { test, expect, request } from '@playwright/test';
import { URLS } from '../helpers/config';

/**
 * Smoke basico: verifica que las 4 superficies (api, admin_web, client_app,
 * kitchen_tablet) esten respondiendo. Si alguno falla, todo lo demas falla.
 */
test.describe('Health checks', () => {
  test('Backend responde /health', async () => {
    const ctx = await request.newContext();
    const resp = await ctx.get(`${URLS.api}/health`);
    expect(resp.ok()).toBeTruthy();
    const body = await resp.json();
    expect(body.status).toBe('ok');
    await ctx.dispose();
  });

  test('Backend conecta a Postgres', async () => {
    const ctx = await request.newContext();
    const resp = await ctx.get(`${URLS.api}/health/db`);
    expect(resp.ok()).toBeTruthy();
    const body = await resp.json();
    expect(body.status).toBe('ok');
    expect(body.db).toBe('postgresql');
    expect(body.ingredients).toBeGreaterThan(0);  // seeds aplicados
    await ctx.dispose();
  });

  // NOTA: Flutter web 3.41 usa CanvasKit por defecto: todo el contenido
  // (textos, botones) se pinta en un <canvas>, NO hay DOM accesible con
  // ese texto. Los selectores tipo getByText/getByLabel solo funcionan
  // tras activar el "semantics tree" (clickeando flt-semantics-placeholder
  // o via JS). Por simplicidad, los health tests usan el <title> del
  // documento, que Flutter setea desde MaterialApp.title.
  test('admin_web sirve la SPA', async ({ page }) => {
    await page.goto(URLS.admin);
    await expect(page).toHaveTitle(/HomeChef Pro/i, { timeout: 60000 });
  });

  test('client_app sirve la SPA', async ({ page }) => {
    await page.goto(URLS.client);
    await expect(page).toHaveTitle(/HomeChef|homechef_client/i, { timeout: 60000 });
  });
});
