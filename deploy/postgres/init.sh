#!/bin/bash
# =====================================================================
# HomeChef Pro - Script de inicialización de PostgreSQL (primer boot)
# =====================================================================
# Este script corre una sola vez al crear el volumen del contenedor.
# Ejecuta los archivos .sql en orden alfabético dentro de
#   /docker-entrypoint-initdb.d/10-schema/  (esquema)
#   /docker-entrypoint-initdb.d/20-seed/    (datos de muestra)
# =====================================================================

set -e

echo ">>> HomeChef Pro: aplicando esquema"
for f in /docker-entrypoint-initdb.d/10-schema/*.sql; do
    # Saltar 99_run_all.sql (usa \ir que requiere psql interactivo)
    case "$(basename "$f")" in
        99_run_all.sql) continue ;;
    esac
    echo "  -> $f"
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -f "$f"
done

echo ">>> HomeChef Pro: aplicando seeds"
if [ -d /docker-entrypoint-initdb.d/20-seed ]; then
    for f in /docker-entrypoint-initdb.d/20-seed/*.sql; do
        [ -e "$f" ] || continue
        echo "  -> $f"
        psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -f "$f"
    done
fi

echo ">>> HomeChef Pro: DB lista"
