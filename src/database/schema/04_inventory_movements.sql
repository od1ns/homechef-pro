-- =====================================================================
-- HomeChef Pro - Movimientos de inventario: compras y mermas
-- =====================================================================
-- Dos orígenes de cambio en stock/costo promedio de un ingrediente:
--   1) Compra (entrada)   -> trigger sube stock + recalcula avg cost
--   2) Merma (salida)     -> trigger baja stock
--   (3) Venta (salida)    -> se maneja al confirmar plato como "listo"
--                            vía trigger sobre order_items (ver 06_orders.sql)
-- Todos los movimientos quedan auditables en 'inventory_movements'.
-- =====================================================================

-- ---------------------------------------------------------------------
-- Compras
-- ---------------------------------------------------------------------
CREATE TABLE ingredient_purchases (
    id                    UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    ingredient_id         UUID           NOT NULL REFERENCES ingredients(id),
    presentation_id       UUID           NOT NULL REFERENCES ingredient_presentations(id),

    -- Número de presentaciones compradas (ej. 2 sacos)
    quantity_purchased    NUMERIC(12,4)  NOT NULL,

    -- Precio USD por UNA presentación (ej. $45 por saco)
    unit_price_usd        NUMERIC(12,4)  NOT NULL,

    -- Total = quantity_purchased * unit_price_usd  (snapshot por si cambia luego)
    total_cost_usd        NUMERIC(14,4)  NOT NULL,

    supplier              VARCHAR(160),
    reference             VARCHAR(120),       -- # factura, nota de entrega
    purchased_at          TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    recorded_by           UUID           NOT NULL,   -- AspNetUsers.Id (Admin/Cajero)
    notes                 TEXT,

    CONSTRAINT purchase_qty_positive   CHECK (quantity_purchased > 0),
    CONSTRAINT purchase_price_positive CHECK (unit_price_usd > 0),
    CONSTRAINT purchase_total_match    CHECK (total_cost_usd > 0)
);

CREATE INDEX idx_purchases_ingredient ON ingredient_purchases(ingredient_id);
CREATE INDEX idx_purchases_date       ON ingredient_purchases(purchased_at DESC);
CREATE INDEX idx_purchases_presentation ON ingredient_purchases(presentation_id);

COMMENT ON TABLE ingredient_purchases IS 'Cada compra de ingrediente. Dispara trigger de actualización de stock y costo promedio.';

-- ---------------------------------------------------------------------
-- Mermas (pérdidas)
-- ---------------------------------------------------------------------
CREATE TABLE ingredient_waste (
    id                    UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    ingredient_id         UUID           NOT NULL REFERENCES ingredients(id),

    -- Cantidad perdida en la unidad de uso del ingrediente
    quantity_use_unit     NUMERIC(14,4)  NOT NULL,

    -- Costo estimado al momento de registro (quantity * avg_cost_per_use_unit_usd)
    estimated_cost_usd    NUMERIC(12,4)  NOT NULL,

    reason                VARCHAR(30)    NOT NULL,
    notes                 TEXT,
    recorded_by           UUID           NOT NULL,
    recorded_at           TIMESTAMPTZ    NOT NULL DEFAULT NOW(),

    CONSTRAINT waste_reason_valid CHECK (
        reason IN ('spoiled','burnt','dropped','expired','over_prepped','theft','other')
    ),
    CONSTRAINT waste_qty_positive   CHECK (quantity_use_unit > 0),
    CONSTRAINT waste_cost_nonneg    CHECK (estimated_cost_usd >= 0)
);

CREATE INDEX idx_waste_ingredient ON ingredient_waste(ingredient_id);
CREATE INDEX idx_waste_date       ON ingredient_waste(recorded_at DESC);
CREATE INDEX idx_waste_reason     ON ingredient_waste(reason);

COMMENT ON TABLE ingredient_waste IS 'Mermas: pérdidas registradas manualmente. Dispara trigger de decremento de stock.';

-- ---------------------------------------------------------------------
-- Libro mayor de movimientos (auditoría unificada)
-- ---------------------------------------------------------------------
-- Todos los cambios de stock (positivos y negativos) quedan aquí con su origen.
-- Poblado por triggers. Útil para reportes y reconciliación.
-- ---------------------------------------------------------------------
CREATE TABLE inventory_movements (
    id                    UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    ingredient_id         UUID           NOT NULL REFERENCES ingredients(id),
    movement_type         VARCHAR(20)    NOT NULL,  -- 'purchase'|'waste'|'sale'|'adjustment'
    quantity_use_unit     NUMERIC(14,4)  NOT NULL,  -- positivo = entrada; negativo = salida
    cost_impact_usd       NUMERIC(14,4)  NOT NULL DEFAULT 0,
    source_table          VARCHAR(40)    NOT NULL,  -- tabla de origen (purchases, waste, order_items, etc.)
    source_id             UUID,                      -- id del registro origen
    resulting_stock       NUMERIC(14,4)  NOT NULL,   -- stock DESPUÉS del movimiento
    resulting_avg_cost    NUMERIC(14,6)  NOT NULL,
    occurred_at           TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    notes                 TEXT,

    CONSTRAINT move_type_valid CHECK (
        movement_type IN ('purchase','waste','sale','adjustment','initial')
    )
);

CREATE INDEX idx_movements_ingredient_date ON inventory_movements(ingredient_id, occurred_at DESC);
CREATE INDEX idx_movements_type            ON inventory_movements(movement_type);
CREATE INDEX idx_movements_source          ON inventory_movements(source_table, source_id);

COMMENT ON TABLE inventory_movements IS 'Auditoría de todos los cambios de stock. Poblada por triggers.';
