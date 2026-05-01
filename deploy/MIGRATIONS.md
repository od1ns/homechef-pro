# Migraciones SQL incrementales

El stack inicial corre `99_run_all.sql` **una sola vez** sobre una base de datos
vacía (lo dispara `init.sh` cuando Docker prende el volumen por primera vez).
Cuando agregamos archivos SQL nuevos al schema (`12_invoices.sql`, `13_*.sql`,
etc.), Postgres no los aplica automáticamente — hay que ejecutarlos a mano sobre
el volumen existente.

## TL;DR

```bash
# Local (laptop con Docker)
./scripts/apply-migration.sh 12_invoices.sql
./scripts/apply-migration.sh 13_customer_preferences.sql

# VPS de producción (vía SSH al chef)
ssh root@hcp.example.com 'cd /opt/homechef-pro && \
    docker compose exec -T postgres psql -U homechef -d homechef \
        < src/database/schema/12_invoices.sql'
```

El script es idempotente sólo si los archivos usan `CREATE TABLE IF NOT EXISTS`,
`CREATE OR REPLACE FUNCTION`, etc. Los nuestros (12, 13) usan `CREATE TABLE`
puro, así que correrlos dos veces falla con "relation already exists" — eso es
esperado.

## Migraciones del 2026-04-25

### `12_invoices.sql` — facturación SENIAT/IGTF

Agrega la tabla `invoices` con snapshot tributario (subtotal/iva/igtf/total),
datos del emisor + cliente, número fiscal + control + provider response, FSM
draft/issued/cancelled/failed.

Aplica si: ya tenías el stack corriendo antes del 2026-04-25 con órdenes
`delivered` que ahora quieres facturar.

```bash
./scripts/apply-migration.sh 12_invoices.sql
```

Verifica:

```bash
docker compose exec postgres psql -U homechef -d homechef \
    -c "\d invoices"
docker compose exec postgres psql -U homechef -d homechef \
    -c "SELECT COUNT(*) FROM invoices;"
```

### `13_customer_preferences.sql` — sync del onboarding

Tabla `customer_preferences(user_id, payload jsonb, updated_at)` para que las
respuestas del onboarding del cliente persistan entre dispositivos.

```bash
./scripts/apply-migration.sh 13_customer_preferences.sql
```

## Qué hacer si la tabla ya existe

Si en algún arranque el contenedor sí ejecutó el archivo (porque el volumen
estaba vacío y el SQL ya estaba en el repo), correrlo otra vez fallará con
`relation "invoices" already exists`. Esto es seguro de ignorar — no se aplican
cambios. Para verificar:

```bash
docker compose exec postgres psql -U homechef -d homechef \
    -c "\dt" | grep -E "(invoices|customer_preferences)"
```

## Convenciones para futuras migraciones

1. Cada archivo nuevo debe ir en `src/database/schema/{N}_{name}.sql` con `N`
   monotónicamente creciente.
2. Agregarlo al final del `99_run_all.sql` para que despliegues nuevos lo
   incluyan.
3. Documentar acá cómo aplicar al volumen existente.
4. Evitar `DROP TABLE` o cambios destructivos en archivos numerados — esos van
   en archivos `M_alter_*.sql` separados con un comentario explícito de "este
   archivo es destructivo, ejecutar con cuidado".

Cuando llegue el momento de migraciones de schema más complejas (índices
únicos en datos existentes, columnas con default no-trivial, particionado),
considerar moverse a una herramienta como
[`migrate`](https://github.com/golang-migrate/migrate) o
[Flyway](https://flywaydb.org/) en lugar del bash artesanal.
