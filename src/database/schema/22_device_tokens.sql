-- =====================================================================
-- HomeChef Pro - Etapa 5: tokens FCM para notificaciones push
-- =====================================================================
-- Un token por pedido. Si el cliente reinstala la app o cambia de
-- dispositivo, el nuevo token reemplaza al anterior (ON CONFLICT UPDATE).
-- =====================================================================

CREATE TABLE IF NOT EXISTS order_device_tokens (
    id         UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id   UUID        NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    fcm_token  TEXT        NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_order_device_token UNIQUE (order_id)
);

COMMENT ON TABLE order_device_tokens IS
    'Token FCM (Firebase Cloud Messaging) del dispositivo del cliente para cada pedido. Permite enviar push cuando el estado del pedido cambia.';
