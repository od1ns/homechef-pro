-- =====================================================================
-- HomeChef Pro - Pedidos (clientes registrados o invitados)
-- =====================================================================
-- Flujo de estados:
--   pending_payment       -> cliente aún debe subir comprobante
--   payment_verifying     -> comprobante subido, admin revisa
--   paid                  -> admin aprobó, pasa a cocina
--   in_preparation        -> cocinero marcó "iniciado" en tablet
--   ready                 -> platos listos
--   in_delivery           -> entregado a repartidor (third_party)
--   delivered             -> cliente lo recibió / retiró
--   cancelled             -> cancelado antes de preparar
--   rejected              -> admin rechazó comprobante de pago
-- =====================================================================

-- Clientes invitados (sin registro) — solo nombre + teléfono para el pedido
CREATE TABLE guest_customers (
    id           UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    full_name    VARCHAR(160)   NOT NULL,
    phone        VARCHAR(30)    NOT NULL,
    created_at   TIMESTAMPTZ    NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_guest_customers_phone ON guest_customers(phone);

COMMENT ON TABLE guest_customers IS 'Clientes que pidieron sin registrarse. Permite historial mínimo por teléfono.';

-- ---------------------------------------------------------------------
-- Pedidos
-- ---------------------------------------------------------------------
CREATE TABLE orders (
    id                            UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    order_number                  VARCHAR(24)    NOT NULL UNIQUE,  -- HC-YYYYMMDD-NNNN

    -- F-24 (audit Pasada B): token anti-IDOR para que clientes anonymous puedan
    -- consultar /api/client/orders/{id}?token=... sin que el solo conocer el GUID
    -- alcance. 24 bytes random hex = 48 chars, 192 bits de entropia.
    access_token                  VARCHAR(64)    NOT NULL UNIQUE
                                  DEFAULT encode(gen_random_bytes(24), 'hex'),


    customer_type                 VARCHAR(20)    NOT NULL,         -- 'registered' | 'guest'
    user_id                       UUID,                             -- AspNetUsers.Id (NULL si invitado)
    guest_customer_id             UUID           REFERENCES guest_customers(id),

    status                        VARCHAR(24)    NOT NULL DEFAULT 'pending_payment',

    delivery_type                 VARCHAR(20)    NOT NULL,         -- 'pickup' | 'third_party'
    delivery_address              TEXT,                             -- solo third_party
    delivery_instructions         TEXT,
    contact_phone                 VARCHAR(30),                      -- snapshot

    scheduled_for                 TIMESTAMPTZ,                      -- NULL = "para ya"
    prep_estimated_ready_at       TIMESTAMPTZ,                      -- calculado al crear

    customer_notes                TEXT,                             -- notas globales del pedido

    -- Totales en USD (moneda base)
    subtotal_usd                  NUMERIC(12,4)  NOT NULL DEFAULT 0,
    discount_usd                  NUMERIC(12,4)  NOT NULL DEFAULT 0,
    delivery_fee_usd              NUMERIC(12,4)  NOT NULL DEFAULT 0,
    total_usd                     NUMERIC(12,4)  NOT NULL DEFAULT 0,

    -- Snapshot de tasa y equivalente en VES al momento del pedido
    exchange_rate_id              UUID           REFERENCES exchange_rates(id),
    rate_ves_per_usd_at_order     NUMERIC(14,4),
    total_ves_at_order_time       NUMERIC(16,2),

    created_at                    TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    updated_at                    TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    paid_at                       TIMESTAMPTZ,
    prep_started_at               TIMESTAMPTZ,
    ready_at                      TIMESTAMPTZ,
    delivered_at                  TIMESTAMPTZ,
    cancelled_at                  TIMESTAMPTZ,
    cancellation_reason           TEXT,

    CONSTRAINT order_customer_ref CHECK (
        (customer_type = 'registered' AND user_id IS NOT NULL AND guest_customer_id IS NULL)
        OR (customer_type = 'guest'    AND user_id IS NULL AND guest_customer_id IS NOT NULL)
    ),
    CONSTRAINT order_status_valid CHECK (status IN (
        'pending_payment','payment_verifying','paid','in_preparation',
        'ready','in_delivery','delivered','cancelled','rejected'
    )),
    CONSTRAINT order_delivery_type_valid CHECK (delivery_type IN ('pickup','third_party')),
    CONSTRAINT order_totals_nonneg CHECK (
        subtotal_usd >= 0 AND discount_usd >= 0 AND delivery_fee_usd >= 0 AND total_usd >= 0
    ),
    CONSTRAINT order_delivery_address CHECK (
        delivery_type <> 'third_party' OR delivery_address IS NOT NULL
    )
);

CREATE INDEX idx_orders_status          ON orders(status);
CREATE INDEX idx_orders_user            ON orders(user_id)           WHERE user_id IS NOT NULL;
CREATE INDEX idx_orders_guest           ON orders(guest_customer_id) WHERE guest_customer_id IS NOT NULL;
CREATE INDEX idx_orders_created         ON orders(created_at DESC);
CREATE INDEX idx_orders_scheduled       ON orders(scheduled_for) WHERE scheduled_for IS NOT NULL;
CREATE INDEX idx_orders_status_created  ON orders(status, created_at DESC);
CREATE INDEX idx_orders_access_token    ON orders(access_token);  -- F-24

COMMENT ON TABLE orders IS 'Pedidos de clientes. FSM de estados desde pending_payment hasta delivered/cancelled.';

-- ---------------------------------------------------------------------
-- Ítems del pedido
-- ---------------------------------------------------------------------
CREATE TABLE order_items (
    id                   UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id             UUID           NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    dish_id              UUID           NOT NULL REFERENCES recipes(id),

    -- Snapshot del nombre y precio al momento del pedido (histórico inmutable)
    dish_name_snapshot   VARCHAR(200)   NOT NULL,
    unit_price_usd       NUMERIC(12,4)  NOT NULL,

    quantity             INT            NOT NULL,
    line_total_usd       NUMERIC(12,4)  NOT NULL,

    item_notes           TEXT,                                       -- "sin cebolla", etc.

    -- Estado por-ítem en cocina (un plato puede estar listo antes que otros)
    kitchen_status       VARCHAR(16)    NOT NULL DEFAULT 'pending',  -- 'pending' | 'in_prep' | 'ready'
    prep_started_at      TIMESTAMPTZ,
    prep_completed_at    TIMESTAMPTZ,

    CONSTRAINT order_item_qty_positive      CHECK (quantity > 0),
    CONSTRAINT order_item_price_nonneg      CHECK (unit_price_usd >= 0),
    CONSTRAINT order_item_total_nonneg      CHECK (line_total_usd >= 0),
    CONSTRAINT order_item_kitchen_status    CHECK (kitchen_status IN ('pending','in_prep','ready'))
);

CREATE INDEX idx_order_items_order          ON order_items(order_id);
CREATE INDEX idx_order_items_dish           ON order_items(dish_id);
CREATE INDEX idx_order_items_kitchen_status ON order_items(kitchen_status)
    WHERE kitchen_status IN ('pending','in_prep');

COMMENT ON TABLE  order_items                IS 'Líneas de pedido con snapshots de nombre/precio al momento de la compra.';
COMMENT ON COLUMN order_items.item_notes     IS 'Notas libres del cliente (sin costo adicional). Visibles en tablet de cocina.';
