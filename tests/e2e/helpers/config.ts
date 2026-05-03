/** URLs y credenciales para los tests. Alineadas con el smoke.ps1. */
export const URLS = {
  api: process.env.HCP_API_BASE ?? 'http://localhost:8080',
  admin: process.env.HCP_ADMIN_URL ?? 'http://localhost:8090',
  client: process.env.HCP_CLIENT_URL ?? 'http://localhost:8091',
  kitchen: process.env.HCP_KITCHEN_URL ?? 'http://localhost:8092',
} as const;

export const CREDENTIALS = {
  admin: {
    email: 'admin@homechef.local',
    password: 'demo1234',
  },
  client: {
    email: 'maria@example.com',
    password: 'demo1234',
  },
} as const;
