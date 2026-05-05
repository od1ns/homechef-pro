-- =====================================================================
-- HomeChef Pro - Tasas de cambio USD/VES
-- =====================================================================
-- El admin ingresa la tasa diaria manualmente.
-- La tasa del día se usa para:
--  * Mostrar precio equivalente en VES en el menú del cliente
--  * Convertir pagos recibidos en VES a USD para contabilidad
--  * Snapshot al momento del pedido (orders.exchange_rate_id)
-- =====================================================================

CREATE TABLE exchange_rates (
    id                  UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    chef_id                       UUID           NOT NULL REFERENCES chefs(id) DEFAULT '00000000-0000-0000-0000-000000000001',
    rate_ves_per_usd    NUMERIC(14,4)  NOT NULL,   -- bolívares por 1 USD
    effective_date      DATE           NOT NULL,
    set_by              UUID           NOT NULL,   -- AspNetUsers.Id (Admin)
    notes               TEXT,
    created_at          TIMESTAMPTZ    NOT NULL DEFAULT NOW(),

    CONSTRAINT exchange_rate_positive CHECK (rate_ves_per_usd > 0),
    -- Una sola tasa por día. Para cambiarla se actualiza la existente.
    -- Pasada C / H-02: per-chef. Cada chef puede usar su propia tasa.
    -- (H-09 lo marca aplazable pero el constraint debe ser correcto desde ya).
    UNIQUE (chef_id, effective_date)
);

CREATE INDEX idx_exchange_rates_date ON exchange_rates(effective_date DESC);

COMMENT ON TABLE exchange_rates           IS 'Tasa USD->VES ingresada manualmente por el admin cada día.';
COMMENT ON COLUMN exchange_rates.rate_ves_per_usd IS 'Cantidad de bolívares equivalente a 1 USD.';
