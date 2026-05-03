import { request, APIRequestContext } from '@playwright/test';
import { URLS, CREDENTIALS } from './config';

/**
 * Helper para hacer setup contra el backend antes de los tests UI:
 * registrar Maria si no existe, asegurar que admin esté operativo, etc.
 */
export async function ensureMariaExists(): Promise<void> {
  const ctx = await request.newContext({ baseURL: URLS.api });
  // Intentar login. Si falla con 401, registrar.
  const loginResp = await ctx.post('/api/auth/login', {
    data: CREDENTIALS.client,
  });
  if (loginResp.status() === 200) {
    await ctx.dispose();
    return;
  }

  // Registrar
  await ctx.post('/api/auth/register', {
    data: {
      email: CREDENTIALS.client.email,
      password: CREDENTIALS.client.password,
      fullName: 'Maria Rodriguez',
      phone: '+58 414 1234567',
    },
  });
  await ctx.dispose();
}

export async function loginAdminApi(): Promise<{
  ctx: APIRequestContext;
  token: string;
}> {
  const ctx = await request.newContext({ baseURL: URLS.api });
  const resp = await ctx.post('/api/auth/login', { data: CREDENTIALS.admin });
  const body = await resp.json();
  return { ctx, token: body.accessToken as string };
}
