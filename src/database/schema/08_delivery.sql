-- =====================================================================
-- HomeChef Pro - Tracking de delivery (webhook genérico)
-- =====================================================================
-- Acepta eventos de cualquier proveedor (Yummy, Ridery, PedidosYa, etc.).
-- Cada evento queda inmutable en 'delivery_events'. El estado normalizado
-- actual se proyecta en 'delivery_tracking' (1 fila por pedido).
-- =====================================================================

-- Snapshot del estado de tracking activo por pedido
CREATE TABLE delivery_tracking (
    id                      UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id                UUID           NOT NULL UNIQUE REFERENCES orders(id) ON DELETE CASCADE,

    provider                VARCHAR(60)    NOT NULL,        -- 'yummy','ridery','manual',...
    external_tracking_id    VARCHAR(120),
    current_status          VARCHAR(24)    NOT NULL DEFAULT 'assigned',
    -- 'assigned' | 'picked_up' | 'on_the_way' | 'delivered' | 'failed' | 'cancelled' | 'unknown'

    courier_name            VARCHAR(120),
    courier_phone           VARCHAR(30),
    courier_vehicle         VARCHAR(60),

    last_known_lat          NUMERIC(9,6),
    last_known_lng          NUMERIC(9,6),
    last_event_at           TIMESTAMPTZ,

    created_at              TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ    NOT NULL DEFAULT NOW(),

    CONSTRAINT delivery_status_valid CHECK (current_status IN (
        'assigned','picked_up','on_the_way','delivered','failed','cancelled','unknown'
    ))
);

CREATE INDEX idx_delivery_tracking_provider    ON delivery_tracking(provider);
CREATE INDEX idx_delivery_tracking_status      ON delivery_tracking(current_status);
CREATE INDEX idx_delivery_tracking_external    ON delivery_tracking(provider, external_tracking_id);

COMMENT ON TABLE delivery_tracking IS 'Estado actual del envío. 1 fila por pedido (con tracking third-party).';

-- ---------------------------------------------------------------------
-- Eventos inmutables (log completo para auditar y depurar)
-- ---------------------------------------------------------------------
CREATE TABLE delivery_events (
    id                      UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id                UUID           NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    provider                VARCHAR(60)    NOT NULL,
    external_tracking_id    VARCHAR(120),

    -- Estado ya normalizado al vocabulario interno
    normalized_status       VARCHAR(24)    NOT NULL,

    -- Estado tal como lo envió el proveedor
    raw_status              VARCHAR(60),

    -- Payload JSON completo recibido vía webhook
    raw_payload             JSONB          NOT NULL,

    -- Firma HMAC recibida (si el proveedor la envía) para auditoría
    signature               TEXT,
    signature_valid         BOOLEAN,

    received_at             TIMESTAMPTZ    NOT NULL DEFAULT NOW(),

    CONSTRAINT delivery_event_status_valid CHECK (normalized_status IN (
        'assigned','picked_up','on_the_way','delivered','failed','cancelled','unknown'
    ))
);

CREATE INDEX idx_delivery_events_order     ON delivery_events(order_id, received_at DESC);
CREATE INDEX idx_delivery_events_provider  ON delivery_events(provider, received_at DESC);
CREATE INDEX idx_delivery_events_status    ON delivery_events(normalized_status);

COMMENT ON TABLE delivery_events IS 'Log inmutable de eventos crudos del webhook de delivery, con mapeo al vocabulario interno.';
