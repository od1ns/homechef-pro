<#
.SYNOPSIS
    Seedea historia rica: 8 clientes registrados + ~100 órdenes distribuidas
    en los últimos 60 días con patrones realistas (picos vie-sáb noche).

.DESCRIPTION
    Prerequisito: backend activo + smoke.ps1 + seed-purchases.ps1 corridos
    antes (para tener admin, María, ingredientes con stock).

    Crea clientes nuevos vía API (con sus refresh tokens, perfil, etc.)
    y después usa SQL directo para insertar órdenes con created_at en el
    pasado — la API no permite manipular fechas, por eso el SQL.

    Idempotente: si los clientes ya existen, los reusa.

.PARAMETER ApiBase
    Default http://localhost:8080

.EXAMPLE
    pwsh ./scripts/seed-rich-history.ps1
#>
[CmdletBinding()]
param(
    [string]$ApiBase = "http://localhost:8080"
)

$ErrorActionPreference = "Stop"

function Write-Step { param([string]$T) Write-Host ""; Write-Host "=== $T ===" -ForegroundColor Cyan }
function Write-Ok   { param([string]$M) Write-Host "  [OK] $M" -ForegroundColor Green }
function Write-Warn { param([string]$M) Write-Host "  [!!] $M" -ForegroundColor Yellow }

$customers = @(
    @{ email="carlos@example.com"; password="demo1234"; fullName="Carlos Mendez"; phone="+58 414 1110001" },
    @{ email="laura@example.com";  password="demo1234"; fullName="Laura Perez";   phone="+58 414 1110002" },
    @{ email="jose@example.com";   password="demo1234"; fullName="Jose Garcia";   phone="+58 414 1110003" },
    @{ email="ana@example.com";    password="demo1234"; fullName="Ana Ramirez";   phone="+58 414 1110004" },
    @{ email="diego@example.com";  password="demo1234"; fullName="Diego Torres";  phone="+58 414 1110005" },
    @{ email="sofia@example.com";  password="demo1234"; fullName="Sofia Lopez";   phone="+58 414 1110006" },
    @{ email="luis@example.com";   password="demo1234"; fullName="Luis Herrera";  phone="+58 414 1110007" }
)

Write-Step "1. Asegurar 7 clientes adicionales"
$customerIds = @()
foreach ($c in $customers) {
    $body = $c | ConvertTo-Json -Compress
    try {
        $resp = Invoke-RestMethod -Method Post -Uri "$ApiBase/api/auth/register" `
            -ContentType "application/json" -Body $body
        $customerIds += $resp.userId
        Write-Ok "Registrado: $($c.email) -> $($resp.userId.Substring(0,8))"
    } catch {
        # Ya existe -> login
        $loginBody = @{ email=$c.email; password=$c.password } | ConvertTo-Json -Compress
        $resp = Invoke-RestMethod -Method Post -Uri "$ApiBase/api/auth/login" `
            -ContentType "application/json" -Body $loginBody
        $customerIds += $resp.userId
        Write-Ok "Ya existia: $($c.email) -> $($resp.userId.Substring(0,8))"
    }
}

Write-Step "2. Login admin + listar dishes y user IDs disponibles"
$auth = Invoke-RestMethod -Method Post -Uri "$ApiBase/api/auth/login" `
    -ContentType "application/json" `
    -Body (@{ email="admin@homechef.local"; password="demo1234" } | ConvertTo-Json -Compress)
$H = @{ Authorization = "Bearer $($auth.accessToken)" }

# Tomar tambien a Maria del smoke clasico
$mariaLogin = Invoke-RestMethod -Method Post -Uri "$ApiBase/api/auth/login" `
    -ContentType "application/json" `
    -Body (@{ email="maria@example.com"; password="demo1234" } | ConvertTo-Json -Compress)
$customerIds += $mariaLogin.userId

# Listar dishes directo de la BD (mas confiable que el endpoint que tiene
# filtros implicitos como is_on_menu).
$dishesRaw = docker compose -f deploy/docker-compose.yml exec -T postgres psql -U homechef -d homechef -tA -c `
    "SELECT id FROM recipes WHERE is_sub_recipe = FALSE AND is_active = TRUE"
$dishIds = ($dishesRaw -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' })

Write-Ok "$($customerIds.Count) clientes activos"
Write-Ok "$($dishIds.Count) platos disponibles (via SQL directo)"

if ($dishIds.Count -eq 0) {
    Write-Host ""
    Write-Host "[XX] No hay platos en la BD. Aplicaste los seeds (02_sample_recipes.sql)?" -ForegroundColor Red
    exit 1
}

# ---------------------------------------------------------------------
# 3. Generar órdenes históricas vía SQL directo.
# ---------------------------------------------------------------------
Write-Step "3. Insertando ~100 órdenes en los últimos 60 días"

# Patrón realista de picos: viernes 19-21h, sábado 13-15h y 19-21h, domingo 12-14h.
# Hora del día: random pero pesado a esas franjas.

$customerIdsSql = ($customerIds | ForEach-Object { "'$_'::uuid" }) -join ','
$dishIdsSql = ($dishIds | ForEach-Object { "'$_'::uuid" }) -join ','

$sql = @"
DO `$`$
DECLARE
    v_user_id        uuid;
    v_dish_id        uuid;
    v_dish_price     numeric(10,2);
    v_dish_name      text;
    v_total          numeric(10,2);
    v_qty            int;
    v_order_id       uuid;
    v_order_number   text;
    v_created_at     timestamptz;
    v_day_offset     int;
    v_hour           int;
    v_dow            int;
    v_n_items        int;
    v_iter           int;
    v_total_orders   int := 100;
    customer_ids     uuid[] := ARRAY[$customerIdsSql];
    dish_ids         uuid[] := ARRAY[$dishIdsSql];
BEGIN
    FOR v_iter IN 1..v_total_orders LOOP
        -- Distribución temporal: día aleatorio en últimos 60 días.
        v_day_offset := floor(random() * 60)::int;
        v_created_at := NOW() AT TIME ZONE 'America/Caracas' - (v_day_offset || ' days')::interval;
        v_dow := EXTRACT(DOW FROM v_created_at);

        -- Hora pesada por día de la semana:
        --   vie (5), sáb (6): 70% chance de 19-21h
        --   dom (0): 60% chance de 12-14h
        --   resto: 13-15 o 19-21 con peso uniforme
        IF v_dow IN (5, 6) AND random() < 0.7 THEN
            v_hour := 19 + floor(random() * 3)::int;  -- 19, 20, 21
        ELSIF v_dow = 0 AND random() < 0.6 THEN
            v_hour := 12 + floor(random() * 3)::int;
        ELSIF random() < 0.5 THEN
            v_hour := 13 + floor(random() * 3)::int;
        ELSE
            v_hour := 19 + floor(random() * 3)::int;
        END IF;

        v_created_at := date_trunc('day', v_created_at)
                      + (v_hour || ' hours')::interval
                      + (floor(random()*60) || ' minutes')::interval;
        v_created_at := v_created_at AT TIME ZONE 'America/Caracas';

        -- Cliente aleatorio.
        v_user_id := customer_ids[1 + floor(random() * array_length(customer_ids, 1))::int];

        -- Crear orden con order_number unico
        v_order_id := gen_random_uuid();
        v_order_number := 'HC-SEED-' || lpad(v_iter::text, 4, '0');

        INSERT INTO orders (
            id, order_number, customer_type, user_id, delivery_type,
            status, subtotal_usd, total_usd, contact_phone,
            created_at, paid_at, ready_at, delivered_at
        ) VALUES (
            v_order_id, v_order_number, 'registered', v_user_id, 'pickup',
            'paid',  -- temp: lo movemos a delivered al final
            0, 0,    -- temp: los items lo recalculan
            '+58 414 0000000',
            v_created_at,
            v_created_at + interval '5 minutes',
            v_created_at + interval '30 minutes',
            v_created_at + interval '45 minutes'
        );

        -- Agregar 1-3 items aleatorios.
        v_n_items := 1 + floor(random() * 3)::int;
        v_total := 0;
        FOR i IN 1..v_n_items LOOP
            v_dish_id := dish_ids[1 + floor(random() * array_length(dish_ids, 1))::int];
            v_qty := 1 + floor(random() * 3)::int;

            SELECT name, COALESCE(selling_price_usd, 0)
            INTO v_dish_name, v_dish_price
            FROM recipes WHERE id = v_dish_id;

            INSERT INTO order_items (
                id, order_id, dish_id, dish_name_snapshot,
                unit_price_usd, quantity, line_total_usd, kitchen_status
            ) VALUES (
                gen_random_uuid(), v_order_id, v_dish_id, v_dish_name,
                v_dish_price, v_qty, v_dish_price * v_qty, 'ready'
            );

            v_total := v_total + (v_dish_price * v_qty);
        END LOOP;

        -- Actualizar totales y poner status delivered (esto dispara el trigger
        -- de loyalty si user_id NOT NULL).
        UPDATE orders
        SET subtotal_usd = v_total,
            total_usd    = v_total,
            status       = 'delivered'
        WHERE id = v_order_id;
    END LOOP;

    RAISE NOTICE 'Seedeadas % ordenes', v_total_orders;
END `$`$;
"@

# Ejecutar el SQL via docker exec
$tmp = New-TemporaryFile
$sql | Out-File -FilePath $tmp.FullName -Encoding utf8

docker cp $tmp.FullName homechef-postgres:/tmp/seed-history.sql | Out-Null
docker compose -f deploy/docker-compose.yml exec postgres psql -U homechef -d homechef -v ON_ERROR_STOP=1 -f /tmp/seed-history.sql
Remove-Item $tmp.FullName

Write-Step "Resumen"
$count = docker compose -f deploy/docker-compose.yml exec -T postgres psql -U homechef -d homechef -tA -c `
    "SELECT COUNT(*) FROM orders WHERE order_number LIKE 'HC-SEED-%'"
Write-Ok "Ordenes seedeadas: $count"

$loyalty = docker compose -f deploy/docker-compose.yml exec -T postgres psql -U homechef -d homechef -tA -c `
    "SELECT user_id, current_balance, lifetime_earned, level FROM loyalty_accounts ORDER BY lifetime_earned DESC LIMIT 5"
Write-Host ""
Write-Host "Top 5 clientes por lifetime points:" -ForegroundColor Cyan
$loyalty | ForEach-Object { Write-Host "  $_" }

Write-Host ""
Write-Host "Refresh los reportes en admin_web -> Analitica para ver datos densos."
