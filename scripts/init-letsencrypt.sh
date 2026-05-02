#!/usr/bin/env bash
# =====================================================================
# HomeChef Pro - Inicialización de certificados Let's Encrypt
# =====================================================================
# Corrió UNA SOLA VEZ por dominio nuevo. Para renovaciones, el contenedor
# `certbot` del docker-compose.prod.yml ya hace el `certbot renew` en loop.
#
# Pre-requisitos en el VPS:
#   - Docker + compose plugin (igual que para el deploy normal)
#   - El dominio (DOMAIN) apunta vía DNS al IP público del servidor
#   - Puertos 80 y 443 abiertos en el firewall
#
# Uso (en el VPS, dentro de /opt/homechef-pro/):
#   DOMAIN=hcp.example.com EMAIL=admin@example.com ./scripts/init-letsencrypt.sh
#
# Para probar contra el staging de Let's Encrypt sin gastar tu cuota
# diaria de certs reales: agregar STAGING=1 al comando.
# =====================================================================
set -euo pipefail

: "${DOMAIN:?must be set (e.g. hcp.example.com)}"
: "${EMAIL:?must be set (e.g. admin@example.com)}"
STAGING="${STAGING:-0}"
DATA_PATH="./deploy/nginx/certs"
RSA_KEY_SIZE=4096

if [ -d "$DATA_PATH/live/$DOMAIN" ]; then
    read -r -p "Existe un cert para $DOMAIN. Reemplazar? (y/N) " ans
    [[ "$ans" =~ ^[Yy] ]] || exit 0
fi

# 1. Bajar las opciones de TLS recomendadas si no las tenemos.
if [ ! -e "$DATA_PATH/options-ssl-nginx.conf" ] || [ ! -e "$DATA_PATH/ssl-dhparams.pem" ]; then
    echo "[init] Descargando configs TLS recomendadas de certbot..."
    mkdir -p "$DATA_PATH"
    curl -fsSL "https://raw.githubusercontent.com/certbot/certbot/master/certbot-nginx/certbot_nginx/_internal/tls_configs/options-ssl-nginx.conf" \
        > "$DATA_PATH/options-ssl-nginx.conf"
    curl -fsSL "https://raw.githubusercontent.com/certbot/certbot/master/certbot/certbot/ssl-dhparams.pem" \
        > "$DATA_PATH/ssl-dhparams.pem"
fi

# 2. Crear cert dummy temporal para que nginx pueda arrancar (necesita
#    presencia de fullchain.pem/privkey.pem aunque sean falsos).
echo "[init] Creando dummy cert para $DOMAIN ..."
mkdir -p "$DATA_PATH/live/$DOMAIN"
docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.prod.yml run --rm \
    --entrypoint "openssl req -x509 -nodes -newkey rsa:$RSA_KEY_SIZE \
        -days 1 -keyout '/etc/letsencrypt/live/$DOMAIN/privkey.pem' \
        -out '/etc/letsencrypt/live/$DOMAIN/fullchain.pem' \
        -subj '/CN=localhost'" certbot

# 3. Levantar nginx (ya tiene los dummy certs montados).
echo "[init] Levantando nginx con dummy cert..."
docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.prod.yml up -d nginx

# 4. Borrar el dummy y pedir el real.
echo "[init] Borrando dummy y solicitando cert real ($DOMAIN)..."
docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.prod.yml run --rm \
    --entrypoint "rm -rf /etc/letsencrypt/live/$DOMAIN \
        /etc/letsencrypt/archive/$DOMAIN \
        /etc/letsencrypt/renewal/$DOMAIN.conf" certbot

STAGING_FLAG=""
if [ "$STAGING" -ne 0 ]; then
    STAGING_FLAG="--staging"
    echo "[init] *** STAGING MODE — el cert resultante NO será de confianza pública. ***"
fi

docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.prod.yml run --rm \
    --entrypoint "certbot certonly --webroot -w /var/www/certbot \
        $STAGING_FLAG \
        --email $EMAIL \
        -d $DOMAIN \
        --rsa-key-size $RSA_KEY_SIZE \
        --agree-tos \
        --non-interactive \
        --force-renewal" certbot

# 5. Reload de nginx para que tome el cert real.
echo "[init] Reload de nginx..."
docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.prod.yml exec nginx nginx -s reload

echo "[init] Cert inicial OK. El servicio certbot del compose se encarga de las renovaciones (cada 12h)."
echo ""
echo "Proximo paso:"
echo "  - Renombrar deploy/nginx/conf.d/api.conf -> activar el server block 443"
echo "    (descomentar el server { listen 443 ssl ... } y reemplazar DOMAIN_PLACEHOLDER por $DOMAIN)"
echo "  - Comentar la location /api/ del server :80 para forzar redirect a HTTPS."
echo "  - docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.prod.yml exec nginx nginx -s reload"
