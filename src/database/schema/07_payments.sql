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
    chef_id                       UUID           NOT NULL REFERENCES chefs(id) DEFAULT '00000000-0000-0000-0000-000000000001',
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


-- =====================================================================
-- F-23 (audit Pasada B): tabla de uploads de comprobantes
-- =====================================================================
-- Cada POST /api/uploads/payment-proofs crea un row aqui. El cliente
-- recibe { id, url } y debe enviar `proofImageId` (NO `proofImageUrl`)
-- al hacer POST /api/client/orders/{id}/payment.
--
-- El handler de SubmitPaymentProof valida que:
--   - el id existe
--   - claimed_at IS NULL (evita re-uso del mismo comprobante para multiples payments)
-- y al validar, marca el upload como claimed.
--
-- Razon: antes el cliente enviaba `proofImageUrl: string` libre, lo que permitia
-- (a) URLs externas (phishing del admin), (b) re-uso de comprobantes ya aprobados,
-- (c) URLs `javascript:` para XSS. Ahora la unica forma de adjuntar comprobante es
-- via un id retornado por el endpoint de upload.
-- =====================================================================
CREATE TABLE payment_proof_uploads (
    id                       UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    chef_id                       UUID           NOT NULL REFERENCES chefs(id) DEFAULT '00000000-0000-0000-0000-000000000001',
    filename                 VARCHAR(96)  NOT NULL UNIQUE,
    content_type             VARCHAR(64)  NOT NULL,
    size_bytes               BIGINT       NOT NULL CHECK (size_bytes > 0),
    uploaded_at              TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    claimed_at               TIMESTAMPTZ,
    claimed_by_payment_id    UUID         REFERENCES payments(id) ON DELETE SET NULL,

    CONSTRAINT pproof_claim_consistency CHECK (
        (claimed_at IS NULL AND claimed_by_payment_id IS NULL)
        OR (claimed_at IS NOT NULL)
    )
);

CREATE INDEX idx_pproof_uploads_unclaimed
    ON payment_proof_uploads(uploaded_at)
    WHERE claimed_at IS NULL;

COMMENT ON TABLE  payment_proof_uploads IS 'F-23: handles de upload con id; claim al asociar a un Payment.';
COMMENT ON COLUMN payment_proof_uploads.claimed_at IS 'NULL = aun no asociado a Payment. Cron puede limpiar uploads NULL > 24h.';
