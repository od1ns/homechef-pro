-- =====================================================================
-- HomeChef Pro - Reporte de rotación de inventario
-- =====================================================================
-- Métricas por ingrediente:
--   * dias_de_stock        = stock_actual / consumo_diario_promedio
--   * rotacion_anual       = 365 / dias_de_stock
--   * fecha_ultima_compra  = MAX(purchase_date)
--   * fecha_ultimo_consumo = MAX(sale_date) que consumió este insumo
--   * categoria:
--        alta     -> > 12 vueltas/año (mueve cada < 30 dias)
--        media    -> 4 a 12 vueltas/año
--        baja     -> < 4 vueltas/año (más de 90 dias para rotar)
--        inactivo -> sin compras NI consumo en los últimos 60 dias
--
-- Útil para: identificar capital muerto (rotación baja, mucho stock parado)
-- y items críticos (rotación alta, riesgo de quiebre).
-- =====================================================================

CREATE OR REPLACE VIEW ingredient_rotation_report AS
WITH last_90_consumption AS (
    SELECT
        ingredient_id,
        SUM(qty_consumed_use_unit)    AS total_consumed_90d,
        MAX(sale_date)                AS last_consumed_at
    FROM ingredient_daily_consumption
    WHERE sale_date >= CURRENT_DATE - INTERVAL '90 days'
    GROUP BY ingredient_id
),
last_purchase AS (
    SELECT
        ingredient_id,
        MAX(purchased_at)             AS last_purchased_at
    FROM ingredient_purchases
    GROUP BY ingredient_id
)
SELECT
    i.id                                                              AS ingredient_id,
    i.name,
    i.use_unit,
    i.current_stock_use_unit,
    i.avg_cost_per_use_unit_usd,
    (i.current_stock_use_unit * i.avg_cost_per_use_unit_usd)          AS stock_value_usd,

    COALESCE(c.total_consumed_90d, 0)                                 AS consumed_last_90d,
    COALESCE(c.total_consumed_90d, 0) / 90.0                          AS daily_avg_consumption,

    -- dias_de_stock = stock_actual / promedio_diario. NULL si no hay consumo.
    CASE
        WHEN COALESCE(c.total_consumed_90d, 0) > 0
        THEN i.current_stock_use_unit / (c.total_consumed_90d / 90.0)
        ELSE NULL
    END                                                               AS days_of_stock,

    -- rotacion_anual = 365 / dias_de_stock
    CASE
        WHEN COALESCE(c.total_consumed_90d, 0) > 0
             AND i.current_stock_use_unit > 0
        THEN 365.0 / (i.current_stock_use_unit / (c.total_consumed_90d / 90.0))
        ELSE NULL
    END                                                               AS annual_turnover,

    p.last_purchased_at::date                                         AS last_purchased_at,
    c.last_consumed_at                                                AS last_consumed_at,

    CASE
        WHEN (p.last_purchased_at IS NULL OR p.last_purchased_at < CURRENT_DATE - INTERVAL '60 days')
             AND (c.last_consumed_at IS NULL OR c.last_consumed_at < CURRENT_DATE - INTERVAL '60 days')
            THEN 'inactivo'
        WHEN COALESCE(c.total_consumed_90d, 0) <= 0
            THEN 'inactivo'
        WHEN i.current_stock_use_unit / (c.total_consumed_90d / 90.0) <= 30
            THEN 'alta'
        WHEN i.current_stock_use_unit / (c.total_consumed_90d / 90.0) <= 90
            THEN 'media'
        ELSE 'baja'
    END                                                               AS rotation_category
FROM ingredients i
LEFT JOIN last_90_consumption c ON c.ingredient_id = i.id
LEFT JOIN last_purchase       p ON p.ingredient_id = i.id
WHERE i.is_active = TRUE;

COMMENT ON VIEW ingredient_rotation_report IS
'Rotación de inventario por ingrediente: días de stock, vueltas/año, categoría (alta/media/baja/inactivo).';
