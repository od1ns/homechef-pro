#!/usr/bin/env bash
# Deploy HomeChef Pro to a remote VPS via SSH.
#
# Required env:
#   DEPLOY_HOST       — e.g. user@hcp.example.com
#   DEPLOY_PATH       — e.g. /opt/homechef-pro            (target dir on the VPS)
#   IMAGE_TAG         — e.g. v0.3.1 or latest             (image tag to pull)
# Optional env:
#   IMAGE_REPO        — defaults to ghcr.io/<owner>/homechef-pro-api
#   COMPOSE_FILE      — defaults to docker-compose.yml
#   COMPOSE_OVERRIDE  — defaults to docker-compose.prod.yml
#
# Assumes the VPS has Docker + docker compose plugin and the target dir already
# contains the deploy/.env file (NEVER commit this file).
set -euo pipefail

: "${DEPLOY_HOST:?must be set (e.g. user@hcp.example.com)}"
: "${DEPLOY_PATH:?must be set (e.g. /opt/homechef-pro)}"
: "${IMAGE_TAG:?must be set (e.g. latest or v0.3.1)}"

IMAGE_REPO="${IMAGE_REPO:-ghcr.io/${GITHUB_REPOSITORY_OWNER:-yourname}/homechef-pro-api}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.yml}"
COMPOSE_OVERRIDE="${COMPOSE_OVERRIDE:-docker-compose.prod.yml}"

echo "[deploy] target=${DEPLOY_HOST}:${DEPLOY_PATH}"
echo "[deploy] image=${IMAGE_REPO}:${IMAGE_TAG}"

# 1. Sync the deploy directory (compose files, nginx, init.sh) to the VPS.
rsync -az --delete \
    --exclude '.env' \
    deploy/ "${DEPLOY_HOST}:${DEPLOY_PATH}/deploy/"

# 2. Sync the SQL schema + seed scripts (so the container can re-init on first run).
rsync -az --delete \
    src/database/ "${DEPLOY_HOST}:${DEPLOY_PATH}/src/database/"

# 3. Pull the image and restart the API container only.
ssh "${DEPLOY_HOST}" "cd ${DEPLOY_PATH}/deploy \
    && docker compose -f ${COMPOSE_FILE} -f ${COMPOSE_OVERRIDE} pull api \
    && docker compose -f ${COMPOSE_FILE} -f ${COMPOSE_OVERRIDE} up -d --no-deps api"

# 4. Quick smoke test: hit /health on the VPS (assumes nginx is listening on :80).
ssh "${DEPLOY_HOST}" "curl -fsS http://localhost/health | head -c 200; echo"

echo "[deploy] OK"
