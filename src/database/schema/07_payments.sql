-- =====================================================================
-- HomeChef Pro - Pagos con verificación manual
-- =====================================================================
-- Métodos soportados: Pago Móvil, Transferencia VES, Transferencia USD,
-- Zelle, Binance Pay, Efectivo (retiro en local).
--
-- Flujo:
--   1) Cliente sube comprobante (imagen) tras hacer el pedido
--   2) Pago queda en status 'pending'
--   3) Admin revisa desde panel y marca 'verified' o 'rejected'
--   4) Al verificar: orders.status pasa a 'paid' y se dispara trigger
-- =====================================================================

CREATE TABLE payments (
    id                       UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id                 UUID           NOT NULL REFERENCES orders(id) ON DELETE CASCADE,

    method                   VARCHAR(24)    NOT NULL,
    -- 'pago_movil' | 'transfer_ves' | 'transfer_usd' | 'zelle' | 'binance_pay' | 'cash'

    -- Monto equivalente en USD (moneda base contable)
    amount_usd               NUMERIC(12,4)  NOT NULL,

    -- Moneda real en que se pagó
    paid_currency            CHAR(3)        NOT NULL,   -- 'USD' | 'VES'
    amount_paid_currency     NUMERIC(16,2)  NOT NULL,
    exchange_rate_used       NUMERIC(14,4),             -- obligatorio si paid_currency = 'VES'

    reference_number         VARCHAR(80),               -- # de confirmación Pago Móvil, Zelle, etc.
    proof_image_url          TEXT,                      -- URL a imagen subida (S3/local)
    payer_name               VARCHAR(160),
    payer_phone              VARCHAR(30),
    payer_account_last4      VARCHAR(10),               -- opcional para transferencias

    status                   VARCHAR(16)    NOT NULL DEFAULT 'pending',
    -- 'pending' | 'verified' | 'rejected'

    verified_by              UUID,                       -- AspNetUsers.Id del admin
    verified_at              TIMESTAMPTZ,
    rejection_reason         TEXT,

    created_at               TIMESTAMPTZ    NOT NULL DEFAULT NOW(),

    CONSTRAINT payment_method_valid CHECK (method IN (
        'pago_movil','transfer_ves','transfer_usd','zelle','binance_pay','cash'
    )),
    CONSTRAINT payment_currency_valid   CHECK (paid_currency IN ('USD','VES')),
    CONSTRAINT payment_status_valid     CHECK (status IN ('pending','verified','rejected')),
    CONSTRAINT payment_ves_needs_rate   CHECK (paid_currency <> 'VES' OR exchange_rate_used IS NOT NULL),
    CONSTRAINT payment_amount_positive  CHECK (amount_usd > 0 AND amount_paid_currency > 0),
    CONSTRAINT payment_verified_fields  CHECK (
        (status = 'verified' AND verified_by IS NOT NULL AND verified_at IS NOT NULL)
        OR status <> 'verified'
    ),
    CONSTRAINT payment_rejected_fields  CHECK (
        (status = 'rejected' AND rejection_reason IS NOT NULL)
        OR status <> 'rejected'
    )
);

CREATE INDEX idx_payments_order      ON payments(order_id);
CREATE INDEX idx_payments_status     ON payments(status);
CREATE INDEX idx_payments_method     ON payments(method);
CREATE INDEX idx_payments_created    ON payments(created_at DESC);
CREATE INDEX idx_payments_pending    ON payments(status, created_at)
    WHERE status = 'pending';

COMMENT ON TABLE  payments                IS 'Comprobantes de pago con verificación manual del admin.';
COMMENT ON COLUMN payments.proof_image_url IS 'Imagen (capture) subida por cliente. Admin la revisa para aprobar.';
