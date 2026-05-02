# Deploy HomeChef Pro

This document explains how to bring up the production stack on a single Linux VPS
(Hetzner / DigitalOcean / Contabo, ~$6–10/mo per [decision #17][decisions]).

## 1. Prerequisites on the VPS

```bash
# 1.1 Install Docker + compose plugin
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker "$USER"

# 1.2 Open ports 80 + 443
sudo ufw allow 22 && sudo ufw allow 80 && sudo ufw allow 443
sudo ufw enable
```

## 2. Project layout on the VPS

The `scripts/deploy.sh` script syncs the repo to:

```
/opt/homechef-pro/
├── deploy/
│   ├── docker-compose.yml          # synced
│   ├── docker-compose.prod.yml     # synced
│   ├── Dockerfile                  # synced
│   ├── nginx/                      # synced
│   ├── init.sh                     # synced
│   └── .env                        # NEVER synced — must be created locally on the VPS
└── src/database/                   # synced (schema + seed for first run)
```

## 3. The `.env` file on the VPS

Create `/opt/homechef-pro/deploy/.env` directly on the VPS (don't commit it):

```bash
# Postgres
POSTGRES_USER=homechef
POSTGRES_PASSWORD=<long-random>
POSTGRES_DB=homechef

# JWT — minimum 32 chars; rotate by restart
JWT_SIGNING_KEY=<random 48 bytes base64>

# Bootstrap the very first admin (clear afterwards if you want)
BOOTSTRAP_ADMIN_EMAIL=admin@yourdomain.com
BOOTSTRAP_ADMIN_PASSWORD=<strong>
BOOTSTRAP_ADMIN_FULLNAME="Tu Nombre"

# Image tag pinning (deploy.sh picks this up via IMAGE_TAG env, but compose can
# also reference ${IMAGE_TAG} if you wire it into the prod override file)
IMAGE_TAG=latest
```

Generate `JWT_SIGNING_KEY` locally:

```bash
openssl rand -base64 48
```

## 4. First deploy

From your laptop, after building/pushing the image via the `release` workflow
(or manually with `docker build` + `docker push`):

```bash
export DEPLOY_HOST=root@hcp.example.com
export DEPLOY_PATH=/opt/homechef-pro
export IMAGE_TAG=latest
export IMAGE_REPO=ghcr.io/<your-org>/homechef-pro-api
./scripts/deploy.sh
```

The first run will:
1. rsync the compose files + SQL scripts.
2. `docker compose pull api` (pulls the image from GHCR).
3. `docker compose up -d --no-deps api` (restart the API only).
4. curl `/health` for a smoke test.

The first time you bring `postgres` + `redis` + `api` up together, run on the VPS:

```bash
cd /opt/homechef-pro/deploy
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

The entrypoint applies the SQL schema/seed files automatically (see `init.sh`).

## 5. Subsequent deploys

```bash
# Tag a release locally, push the tag, and let `release.yml` build the image:
git tag v0.3.2
git push origin v0.3.2

# Once the GHCR image is published, deploy:
IMAGE_TAG=v0.3.2 ./scripts/deploy.sh
```

## 6. Rolling back

```bash
IMAGE_TAG=v0.3.1 ./scripts/deploy.sh
```

Database changes are managed by SQL scripts — there is no EF migration step
to roll back. If a release ships schema changes, also run the corresponding
revert script before re-pulling the previous image.

## 7. Migraciones SQL incrementales

Cuando el repo agrega un archivo SQL nuevo (ej. `12_invoices.sql`), el container
de Postgres no lo aplica automáticamente — `init.sh` corre los scripts una sola
vez sobre el volumen vacío. Para aplicar al volumen existente:

```bash
./scripts/apply-migration.sh 12_invoices.sql
```

Detalles y convenciones en [`deploy/MIGRATIONS.md`](../deploy/MIGRATIONS.md).

## 8. HTTPS (Let's Encrypt)

El stack de prod ya incluye un container `certbot` que renueva certs cada 12 h.
La emisión inicial es manual:

```bash
# En el VPS, dentro de /opt/homechef-pro/
DOMAIN=hcp.example.com EMAIL=admin@example.com ./scripts/init-letsencrypt.sh
```

Pre-requisitos:

- DNS apuntando al servidor (`A`/`AAAA` records).
- Puertos 80 y 443 abiertos al mundo.
- Quincenas restantes en la cuota de Let's Encrypt (50/sem por dominio).

Para probar el flujo sin tocar la cuota real, agregar `STAGING=1`:

```bash
STAGING=1 DOMAIN=staging.hcp.example.com EMAIL=admin@example.com \
    ./scripts/init-letsencrypt.sh
```

Tras el primer `init-letsencrypt.sh`:

1. Editar `deploy/nginx/conf.d/api.conf` y reemplazar `DOMAIN_PLACEHOLDER` por el dominio real (en el server block 443 que viene comentado).
2. Descomentar el server block 443.
3. Comentar el `location /api/` del server `:80` y descomentar el `return 301 https://$host$request_uri;`.
4. Reload nginx:

```bash
docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.prod.yml \
    exec nginx nginx -s reload
```

Verificar con:

```bash
curl -I https://hcp.example.com/health
# 200 OK + headers TLS
```

## 9. Backups

```bash
# On the VPS — daily cron at 03:00:
docker compose exec -T postgres \
    pg_dump -U homechef homechef \
    | gzip > /opt/backups/homechef-$(date +%F).sql.gz

# Retain 30 days
find /opt/backups -name "homechef-*.sql.gz" -mtime +30 -delete
```

[decisions]: ../README.md
