import { defineConfig, devices } from '@playwright/test';

/**
 * Pre-requisitos para correr estos tests:
 *   1. Backend en docker:    cd deploy && docker compose up -d
 *   2. admin_web Flutter:    cd src/frontend/admin_web   && flutter run -d web-server --web-port=8090 --dart-define=HCP_API_BASE=http://localhost:8080
 *   3. client_app Flutter:   cd src/frontend/client_app  && flutter run -d web-server --web-port=8091 --dart-define=HCP_API_BASE=http://localhost:8080
 *   4. seed-purchases:       pwsh ./scripts/seed-purchases.ps1
 *
 * Después: npm test (desde tests/e2e/)
 */
export default defineConfig({
  testDir: './specs',
  fullyParallel: false,         // Los tests comparten datos del backend; no podemos paralelizar
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
  ],

  use: {
    actionTimeout: 10000,
    navigationTimeout: 30000,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',

    // baseURL no se setea aquí porque cada spec elige entre admin/client/kitchen.
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
