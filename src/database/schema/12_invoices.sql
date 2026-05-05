-- =====================================================================
-- HomeChef Pro - Facturación electrónica (SENIAT/IGTF)
-- =====================================================================
-- Cada Order entregada y pagada puede emitir una factura formal.
-- En Venezuela el SENIAT exige número de control + número de factura
-- correlativo asignado por máquina fiscal o proveedor homologado.
-- Esta tabla guarda el snapshot tributario y referencia al proveedor que
-- emitió. Cuando aún no hay proveedor real conectado, el provider es
-- 'mock' y se asignan números MOCK-{n} para pruebas.
--
-- Impuestos venezolanos (al 2026):
--  * IVA 16%               sobre subtotal
--  * IGTF 3%               sobre el monto pagado en divisas (USD/USDT)
-- =====================================================================

CREATE TABLE invoices (
    id                       UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    chef_id                       UUID           NOT NULL REFERENCES chefs(id) DEFAULT '00000000-0000-0000-0000-000000000001',
    order_id                 UUID           NOT NULL UNIQUE REFERENCES orders(id) ON DELETE RESTRICT,

    -- Snapshot tributario (USD)
    subtotal_usd             NUMERIC(14,4)  NOT NULL,
    iva_usd                  NUMERIC(14,4)  NOT NULL,
    igtf_usd                 NUMERIC(14,4)  NOT NULL DEFAULT 0,
    total_with_tax_usd       NUMERIC(14,4)  NOT NULL,

    iva_rate                 NUMERIC(6,4)   NOT NULL DEFAULT 0.16,
    igtf_rate                NUMERIC(6,4)   NOT NULL DEFAULT 0.03,
    igtf_applies             BOOLEAN        NOT NULL DEFAULT FALSE,

    -- Datos del emisor (snapshot — el chef puede cambiarlos después)
    issuer_rif               VARCHAR(20),       -- 'V-12345678-9' o 'J-12345678-9'
    issuer_legal_name        VARCHAR(200),
    issuer_address           TEXT,

    -- Datos del cliente (opcional para consumidor final)
    customer_rif             VARCHAR(20),
    customer_legal_name      VARCHAR(200),
    customer_address         TEXT,

    -- Datos asignados por el proveedor fiscal
    provider                 VARCHAR(40)    NOT NULL DEFAULT 'mock',  -- 'mock' | 'zcomp' | 'hka' | …
    fiscal_number            VARCHAR(40),       -- número de factura asignado
    control_number           VARCHAR(40),       -- número de control fiscal
    provider_response_json   JSONB,             -- respuesta cruda del proveedor

    status                   VARCHAR(20)    NOT NULL DEFAULT 'draft',
    -- 'draft' | 'issued' | 'cancelled' | 'failed'

    issued_at                TIMESTAMPTZ,
    cancelled_at             TIMESTAMPTZ,
    cancellation_reason      TEXT,

    issued_by                UUID,              -- AspNetUsers.Id (Admin)

    created_at               TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    updated_at               TIMESTAMPTZ    NOT NULL DEFAULT NOW(),

    CONSTRAINT invoice_status_valid CHECK (status IN ('draft','issued','cancelled','failed')),
    CONSTRAINT invoice_subtotal_nonneg CHECK (subtotal_usd >= 0),
    CONSTRAINT invoice_iva_nonneg      CHECK (iva_usd >= 0),
    CONSTRAINT invoice_igtf_nonneg     CHECK (igtf_usd >= 0),
    CONSTRAINT invoice_total_nonneg    CHECK (total_with_tax_usd >= 0),
    CONSTRAINT invoice_issued_has_numbers CHECK (
        status <> 'issued'
        OR (fiscal_number IS NOT NULL AND control_number IS NOT NULL AND issued_at IS NOT NULL)
    ),
    CONSTRAINT invoice_cancelled_has_reason CHECK (
        status <> 'cancelled'
        OR (cancellation_reason IS NOT NULL AND cancelled_at IS NOT NULL)
    )
);

CREATE INDEX idx_invoices_order      ON invoices(order_id);
CREATE INDEX idx_invoices_status     ON invoices(status);
CREATE INDEX idx_invoices_issued     ON invoices(issued_at DESC) WHERE status = 'issued';
CREATE INDEX idx_invoices_provider   ON invoices(provider);
-- Pasada C / H-02: fiscal_number UNIQUE per-chef (cada chef tiene su propia
-- secuencia SENIAT con su provider).
CREATE UNIQUE INDEX uq_invoices_fiscal_number
    ON invoices(chef_id, provider, fiscal_number)
    WHERE fiscal_number IS NOT NULL;

CREATE TRIGGER trg_invoices_touch
    BEFORE UPDATE ON invoices
    FOR EACH ROW EXECUTE FUNCTION fn_touch_updated_at();

COMMENT ON TABLE  invoices                   IS 'Facturas emitidas por orden, con datos tributarios SENIAT (IVA + IGTF)';
COMMENT ON COLUMN invoices.igtf_applies      IS 'TRUE cuando el pago aceptado fue en divisas (USD/USDT/Zelle)';
COMMENT ON COLUMN invoices.provider          IS 'Proveedor fiscal: mock (dev), zcomp, hka, etc.';
COMMENT ON COLUMN invoices.provider_response_json IS 'Respuesta cruda del proveedor para auditoría/depuración';
