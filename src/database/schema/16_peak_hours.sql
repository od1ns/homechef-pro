-- =====================================================================
-- HomeChef Pro - Patrones de demanda por hora y día de la semana
-- =====================================================================
-- Genera dos vistas para el dashboard del chef:
--   * orders_peak_hours_heatmap: matriz 7 días × 24 horas con conteo y
--     revenue por celda. Permite identificar picos visuales (ej: viernes
--     7-9 PM, domingos 12-2 PM).
--   * orders_peak_hour_summary: cuál fue la hora pico cada día de la semana.
--
-- Ambas filtran a los últimos 90 días y excluyen orders cancelled/rejected.
-- Las horas se calculan en zona Caracas (la que usa el negocio).
-- =====================================================================

CREATE OR REPLACE VIEW orders_peak_hours_heatmap AS
SELECT
    EXTRACT(DOW FROM created_at AT TIME ZONE 'America/Caracas')::int   AS day_of_week,
    -- Postgres DOW: 0 = domingo, 6 = sábado.
    EXTRACT(HOUR FROM created_at AT TIME ZONE 'America/Caracas')::int  AS hour_of_day,
    COUNT(*)::int                                                       AS orders_count,
    COALESCE(SUM(total_usd), 0)                                         AS revenue_usd,
    CASE
        WHEN COUNT(*) > 0 THEN AVG(total_usd)
        ELSE 0
    END                                                                 AS avg_ticket_usd
FROM orders
WHERE created_at >= NOW() - INTERVAL '90 days'
  AND status NOT IN ('cancelled', 'rejected')
GROUP BY day_of_week, hour_of_day;

COMMENT ON VIEW orders_peak_hours_heatmap IS
'Heatmap de demanda: orders agrupadas por día de la semana (0=dom, 6=sab) y hora (0-23), zona Caracas.';

-- ---------------------------------------------------------------------
-- Hora pico por día de la semana — para mostrar "los viernes el pico es 8 PM".
-- Selecciona, por cada day_of_week, la hour_of_day con mayor orders_count.
-- ---------------------------------------------------------------------
CREATE OR REPLACE VIEW orders_peak_hour_summary AS
WITH ranked AS (
    SELECT
        day_of_week,
        hour_of_day,
        orders_count,
        revenue_usd,
        ROW_NUMBER() OVER (
            PARTITION BY day_of_week
            ORDER BY orders_count DESC, revenue_usd DESC
        ) AS rn
    FROM orders_peak_hours_heatmap
)
SELECT
    day_of_week,
    hour_of_day                                AS peak_hour,
    orders_count                               AS peak_orders_count,
    revenue_usd                                AS peak_revenue_usd
FROM ranked
WHERE rn = 1;

COMMENT ON VIEW orders_peak_hour_summary IS
'Hora pico por día de la semana en los últimos 90 días.';
