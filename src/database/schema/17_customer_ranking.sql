-- =====================================================================
-- HomeChef Pro - Ranking de clientes (RFM)
-- =====================================================================
-- Métricas estándar de retail por cliente:
--   * Recency  -> días desde la última orden (cuanto menor, más activo)
--   * Frequency -> total de órdenes que entregamos
--   * Monetary  -> total gastado de por vida
--
-- Clasificación VIP:
--   * vip       -> ≥10 órdenes y ≥USD 100 lifetime y última en los 30 días
--   * regular   -> ≥3 órdenes y última en los 60 días
--   * casual    -> 1-2 órdenes Y todavía activo (90 días)
--   * dormido   -> última orden hace > 90 días
--
-- Combina clientes registrados (user_profiles) y guests (guest_customers).
-- =====================================================================

CREATE OR REPLACE VIEW customer_ranking AS
WITH order_metrics AS (
    -- Solo contamos las órdenes que llegaron a entregarse (revenue real).
    SELECT
        CASE WHEN customer_type = 'registered' THEN user_id::text
             ELSE guest_customer_id::text END                AS customer_key,
        customer_type,
        user_id,
        guest_customer_id,
        COUNT(*)                                             AS orders_count,
        SUM(total_usd)                                       AS lifetime_spend_usd,
        AVG(total_usd)                                       AS avg_ticket_usd,
        MAX(created_at)                                      AS last_order_at,
        MIN(created_at)                                      AS first_order_at,
        COUNT(*) FILTER (WHERE created_at >= NOW() - INTERVAL '90 days') AS orders_last_90d,
        SUM(total_usd) FILTER (WHERE created_at >= NOW() - INTERVAL '90 days') AS spend_last_90d
    FROM orders
    WHERE status = 'delivered'
    GROUP BY customer_type, user_id, guest_customer_id
)
SELECT
    om.customer_key,
    om.customer_type,
    om.user_id,
    om.guest_customer_id,
    -- Identidad del cliente — desde profile para registered, guest_customer para guest.
    COALESCE(up.full_name, gc.full_name)                      AS display_name,
    COALESCE(au.email, NULL)                                  AS email,
    COALESCE(up.default_phone, gc.phone)                      AS phone,

    om.orders_count,
    om.lifetime_spend_usd,
    om.avg_ticket_usd,

    om.first_order_at,
    om.last_order_at,
    EXTRACT(DAY FROM (NOW() - om.last_order_at))::int         AS days_since_last_order,

    COALESCE(om.orders_last_90d, 0)                           AS orders_last_90d,
    COALESCE(om.spend_last_90d, 0)                            AS spend_last_90d,

    CASE
        WHEN om.orders_count >= 10
             AND om.lifetime_spend_usd >= 100
             AND om.last_order_at >= NOW() - INTERVAL '30 days'
            THEN 'vip'
        WHEN om.orders_count >= 3
             AND om.last_order_at >= NOW() - INTERVAL '60 days'
            THEN 'regular'
        WHEN om.last_order_at >= NOW() - INTERVAL '90 days'
            THEN 'casual'
        ELSE 'dormido'
    END                                                       AS segment
FROM order_metrics om
LEFT JOIN user_profiles    up ON up.user_id = om.user_id
LEFT JOIN asp_net_users    au ON au.id      = om.user_id
LEFT JOIN guest_customers  gc ON gc.id      = om.guest_customer_id;

COMMENT ON VIEW customer_ranking IS
'Ranking de clientes con métricas RFM y segmento (vip/regular/casual/dormido).';
