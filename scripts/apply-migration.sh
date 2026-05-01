#!/usr/bin/env bash
# Apply a single SQL migration file to the running Postgres container.
#
# Usage:
#     ./scripts/apply-migration.sh 12_invoices.sql
#     ./scripts/apply-migration.sh 13_customer_preferences.sql
#
# Resolves the file from src/database/schema/ and pipes it into
# `docker compose exec postgres psql`. Reads connection settings from
# deploy/.env so the credentials match the running container.
set -euo pipefail

cd "$(dirname "$0")/.."

if [[ $# -lt 1 ]]; then
    echo "usage: $0 <NN_filename.sql>"
    exit 1
fi

FILE="$1"
SQL_PATH="src/database/schema/${FILE}"

if [[ ! -f "${SQL_PATH}" ]]; then
    echo "[apply-migration] not found: ${SQL_PATH}"
    exit 1
fi

# Allow override via env, fall back to deploy/.env, finally to defaults.
if [[ -f deploy/.env ]]; then
    # shellcheck disable=SC1091
    set -a; source deploy/.env; set +a
fi
PG_USER="${POSTGRES_USER:-homechef}"
PG_DB="${POSTGRES_DB:-homechef}"

echo "[apply-migration] file=${FILE} target=${PG_USER}@${PG_DB}"

# `docker compose -f deploy/docker-compose.yml` so the script works regardless of cwd.
docker compose -f deploy/docker-compose.yml exec -T postgres \
    psql -v ON_ERROR_STOP=1 -U "${PG_USER}" -d "${PG_DB}" \
    < "${SQL_PATH}"

echo "[apply-migration] OK"
