-- =====================================================================
-- HomeChef Pro - Vistas analíticas y de negocio
-- =====================================================================
-- Vistas provistas:
--   recipe_ingredient_flat         -> cada plato/receta -> ingredientes hoja con qty escalada
--   recipe_full_cost_usd           -> costo total USD por receta (cascada recursiva)
--   dish_profit_margin             -> margen de ganancia por plato
--   ingredient_daily_consumption   -> consumo diario por ingrediente (últimos 90 días)
--   ingredient_reorder_suggestions -> lista de compras sugerida con prioridades
--   kitchen_active_queue           -> cola activa de pedidos en cocina (tablet)
--   sales_daily_summary            -> resumen diario de ventas (dashboard admin)
-- =====================================================================

-- ---------------------------------------------------------------------
-- recipe_ingredient_flat
-- Desenvuelve cada receta (incluyendo sub-recetas anidadas) a sus
-- ingredientes hoja con cantidades proporcionalmente escaladas.
-- ---------------------------------------------------------------------
CREATE OR REPLACE VIEW recipe_ingredient_flat AS
WITH RECURSIVE expanded AS (
    -- Raíz: cada receta genera sus componentes directos con factor de escala 1
    SELECT
        rc.parent_recipe_id                          AS root_recipe_id,
        rc.ingredient_id,
        rc.sub_recipe_id,
        rc.quantity::numeric                         AS scaled_quantity,
        ARRAY[rc.parent_recipe_id]                   AS visited_path,
        0                                            AS depth
    FROM recipe_components rc

    UNION ALL

    -- Descenso: para cada componente que es sub-receta, explotar sus propios
    -- componentes escalando por (cantidad_consumida / rendimiento_subreceta)
    SELECT
        e.root_recipe_id,
        child.ingredient_id,
        child.sub_recipe_id,
        child.quantity * (e.scaled_quantity / NULLIF(sub.yield_quantity, 0)) AS scaled_quantity,
        e.visited_path || child.parent_recipe_id,
        e.depth + 1
    FROM expanded e
    JOIN recipes            sub   ON sub.id = e.sub_recipe_id AND sub.is_sub_recipe = TRUE
    JOIN recipe_components  child ON child.parent_recipe_id = sub.id
    WHERE e.sub_recipe_id IS NOT NULL
      AND NOT (sub.id = ANY(e.visited_path))    -- prevención de ciclos
      AND e.depth < 10                          -- límite defensivo de profundidad
)
SELECT
    root_recipe_id,
    ingredient_id,
    SUM(scaled_quantity) AS total_quantity_use_unit
FROM expanded
WHERE ingredient_id IS NOT NULL
GROUP BY root_recipe_id, ingredient_id;

COMMENT ON VIEW recipe_ingredient_flat IS
'Aplana cada receta a sus ingredientes hoja con cantidades escaladas a través de sub-recetas.';

-- ---------------------------------------------------------------------
-- recipe_full_cost_usd
-- Costo total por receta (suma costo de ingredientes directos + cascada)
-- ---------------------------------------------------------------------
CREATE OR REPLACE VIEW recipe_full_cost_usd AS
SELECT
    r.id                                                                              AS recipe_id,
    r.name,
    r.is_sub_recipe,
    COALESCE(SUM(rif.total_quantity_use_unit * i.avg_cost_per_use_unit_usd), 0)       AS total_cost_usd
FROM recipes r
LEFT JOIN recipe_ingredient_flat rif ON rif.root_recipe_id = r.id
LEFT JOIN ingredients            i   ON i.id = rif.ingredient_id
GROUP BY r.id, r.name, r.is_sub_recipe;

COMMENT ON VIEW recipe_full_cost_usd IS
'Costo USD completo por receta usando costo promedio ponderado actual de cada ingrediente.';

-- ---------------------------------------------------------------------
-- dish_profit_margin
-- Precio de venta vs costo para cada plato final
-- ---------------------------------------------------------------------
CREATE OR REPLACE VIEW dish_profit_margin AS
SELECT
    r.id                                                            AS dish_id,
    r.name,
    r.selling_price_usd,
    rfc.total_cost_usd,
    (r.selling_price_usd - rfc.total_cost_usd)                      AS gross_profit_usd,
    CASE
        WHEN r.selling_price_usd > 0
        THEN ((r.selling_price_usd - rfc.total_cost_usd) / r.selling_price_usd) * 100
        ELSE 0
    END                                                             AS gross_margin_pct,
    CASE
        WHEN rfc.total_cost_usd > 0
        THEN (r.selling_price_usd / rfc.total_cost_usd)
        ELSE NULL
    END                                                             AS price_to_cost_ratio
FROM recipes r
LEFT JOIN recipe_full_cost_usd rfc ON rfc.recipe_id = r.id
WHERE r.is_sub_recipe = FALSE
  AND r.is_active = TRUE;

COMMENT ON VIEW dish_profit_margin IS
'Margen bruto por plato: precio de venta - costo actual. Para sugerir ajustes de precio.';

-- ---------------------------------------------------------------------
-- ingredient_daily_consumption
-- Consumo diario por ingrediente en los últimos 90 días
-- ---------------------------------------------------------------------
CREATE OR REPLACE VIEW ingredient_daily_consumption AS
SELECT
    o.created_at::date                                          AS sale_date,
    rif.ingredient_id,
    SUM(rif.total_quantity_use_unit * oi.quantity)              AS qty_consumed_use_unit
FROM orders o
JOIN order_items             oi  ON oi.order_id = o.id
JOIN recipe_ingredient_flat  rif ON rif.root_recipe_id = oi.dish_id
WHERE o.status = 'delivered'
  AND o.created_at >= NOW() - INTERVAL '90 days'
GROUP BY o.created_at::date, rif.ingredient_id;

COMMENT ON VIEW ingredient_daily_consumption IS
'Consumo diario de cada ingrediente en los últimos 90 días (para predicción de compras).';

-- ---------------------------------------------------------------------
-- ingredient_reorder_suggestions
-- Lista de compras sugerida con prioridad
-- ---------------------------------------------------------------------
CREATE OR REPLACE VIEW ingredient_reorder_suggestions AS
WITH consumption_last_30 AS (
    SELECT
        ingredient_id,
        SUM(qty_consumed_use_unit)                          AS total_consumed_30d,
        COUNT(DISTINCT sale_date)                           AS active_days_30d
    FROM ingredient_daily_consumption
    WHERE sale_date >= CURRENT_DATE - INTERVAL '30 days'
    GROUP BY ingredient_id
)
SELECT
    i.id                                                    AS ingredient_id,
    i.name,
    i.use_unit,
    i.current_stock_use_unit,
    i.reorder_point_use_unit,
    i.minimum_stock_use_unit,
    i.avg_cost_per_use_unit_usd,
    COALESCE(c.total_consumed_30d / NULLIF(c.active_days_30d, 0), 0) AS avg_daily_consumption,
    CASE
        WHEN COALESCE(c.total_consumed_30d, 0) = 0 THEN NULL
        ELSE i.current_stock_use_unit
             / NULLIF(c.total_consumed_30d / NULLIF(c.active_days_30d, 0), 0)
    END                                                     AS estimated_days_until_stockout,
    CASE
        WHEN i.current_stock_use_unit <= i.minimum_stock_use_unit       THEN 'critical'
        WHEN i.current_stock_use_unit <= i.reorder_point_use_unit       THEN 'urgent'
        WHEN COALESCE(c.total_consumed_30d, 0) = 0                      THEN 'ok'
        WHEN i.current_stock_use_unit
             / NULLIF(c.total_consumed_30d / NULLIF(c.active_days_30d, 0), 0) < 3 THEN 'urgent'
        WHEN i.current_stock_use_unit
             / NULLIF(c.total_consumed_30d / NULLIF(c.active_days_30d, 0), 0) < 7 THEN 'soon'
        ELSE 'ok'
    END                                                     AS priority
FROM ingredients i
LEFT JOIN consumption_last_30 c ON c.ingredient_id = i.id
WHERE i.is_active = TRUE;

COMMENT ON VIEW ingredient_reorder_suggestions IS
'Sugerencias de reabastecimiento: días hasta agotamiento y prioridad por ingrediente.';

-- ---------------------------------------------------------------------
-- kitchen_active_queue
-- Cola de cocina (para la tablet): pedidos pagados con ítems no listos
-- ---------------------------------------------------------------------
CREATE OR REPLACE VIEW kitchen_active_queue AS
SELECT
    o.id                        AS order_id,
    o.order_number,
    o.status                    AS order_status,
    o.scheduled_for,
    o.prep_estimated_ready_at,
    o.customer_notes,
    oi.id                       AS order_item_id,
    oi.dish_id,
    oi.dish_name_snapshot,
    oi.quantity,
    oi.item_notes,
    oi.kitchen_status,
    oi.prep_started_at,
    r.procedure_markdown,
    r.prep_time_minutes,
    -- Prioridad: programados atrasados primero, luego FIFO
    COALESCE(o.scheduled_for, o.paid_at, o.created_at)  AS priority_time
FROM orders o
JOIN order_items oi ON oi.order_id = o.id
JOIN recipes     r  ON r.id = oi.dish_id
WHERE o.status IN ('paid', 'in_preparation')
  AND oi.kitchen_status IN ('pending', 'in_prep')
ORDER BY priority_time ASC;

COMMENT ON VIEW kitchen_active_queue IS
'Feed para la tablet de cocina: ítems en preparación ordenados por prioridad temporal.';

-- ---------------------------------------------------------------------
-- sales_daily_summary
-- Resumen diario de ventas (últimos 90 días)
-- ---------------------------------------------------------------------
CREATE OR REPLACE VIEW sales_daily_summary AS
SELECT
    o.created_at::date                                      AS sale_date,
    COUNT(DISTINCT o.id)                                    AS orders_count,
    SUM(o.total_usd)                                        AS revenue_usd,
    SUM(o.total_usd - COALESCE(rfc_total.cost_usd, 0))      AS gross_profit_usd
FROM orders o
LEFT JOIN LATERAL (
    SELECT SUM(rfc.total_cost_usd * oi.quantity) AS cost_usd
    FROM order_items oi
    JOIN recipe_full_cost_usd rfc ON rfc.recipe_id = oi.dish_id
    WHERE oi.order_id = o.id
) rfc_total ON TRUE
WHERE o.status = 'delivered'
  AND o.created_at >= NOW() - INTERVAL '90 days'
GROUP BY o.created_at::date
ORDER BY sale_date DESC;

COMMENT ON VIEW sales_daily_summary IS
'Ventas diarias con ingresos, costos y ganancia bruta (últimos 90 días).';
