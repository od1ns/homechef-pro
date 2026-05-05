-- =====================================================================
-- HomeChef Pro - Tabla `chefs` (multi-tenant root)
-- =====================================================================
-- Pasada C / Fase 1C-A (Bloque 1):
--
-- Cada chef es un inquilino del SaaS. Esta tabla es el ANCLA del modelo
-- multi-tenant: todas las tablas de negocio referencian `chefs.id` via
-- una columna `chef_id`. En la version actual (single-tenant en
-- produccion para el piloto), todas las tablas tienen DEFAULT al UUID
-- determinista del piloto, asi el codigo actual que no setea chef_id
-- sigue funcionando.
--
-- Cuando llegue el segundo chef:
--   1. Quitar los DEFAULTs (ALTER TABLE ... ALTER COLUMN chef_id DROP DEFAULT).
--   2. Agregar global query filter en EF (chef_id == _currentChef.Id).
--   3. Habilitar RLS en cada tabla de negocio.
--
-- Modelo de baja: SOFT DELETE via `status`. Razones:
--   - Las facturas SENIAT deben permanecer accesibles 5+ anos.
--   - Reactivacion del chef no requiere restore de backup.
--   - Reportes historicos del operador del SaaS necesitan datos.
-- =====================================================================

CREATE TABLE chefs (
    id              UUID         PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Identidad fiscal venezolana
    rif             VARCHAR(20)  NOT NULL UNIQUE,    -- 'J-12345678-9'
    legal_name      VARCHAR(200) NOT NULL,           -- razon social SENIAT
    trade_name      VARCHAR(120),                    -- marca/nombre comercial visible al cliente
    tax_address     TEXT         NOT NULL,           -- domicilio fiscal SENIAT

    -- Configuracion operacional
    timezone        VARCHAR(50)  NOT NULL DEFAULT 'America/Caracas',
    base_currency   VARCHAR(3)   NOT NULL DEFAULT 'USD',
    display_currency VARCHAR(3)  NOT NULL DEFAULT 'VES',

    -- Numeracion fiscal: prefix del order_number (HC-YYYYMMDD-NNNN).
    -- Se permite max 4 chars; cada chef tiene su prefix (CHA, CHB, etc).
    invoice_prefix  VARCHAR(4)   NOT NULL DEFAULT 'HC',

    -- Contacto del chef (operador, no del SaaS)
    contact_email   VARCHAR(120),
    contact_phone   VARCHAR(30),

    -- Ciclo de vida del inquilino
    status          VARCHAR(16)  NOT NULL DEFAULT 'active',
                    -- 'active'    -> operativo
                    -- 'suspended' -> bloqueado (impago, incumplimiento) — NO acepta orders
                    -- 'archived'  -> dado de baja (soft delete) — datos preservados, oculto de queries

    created_at      TIMESTAMPTZ  NOT NULL DEFAULT now(),
    activated_at    TIMESTAMPTZ,
    suspended_at    TIMESTAMPTZ,
    archived_at     TIMESTAMPTZ,

    CONSTRAINT chefs_status_chk CHECK (status IN ('active', 'suspended', 'archived')),
    CONSTRAINT chefs_currency_chk CHECK (
        base_currency = 'USD' AND display_currency IN ('USD', 'VES')
    )
);

CREATE INDEX idx_chefs_status ON chefs (status) WHERE status <> 'archived';
CREATE INDEX idx_chefs_invoice_prefix ON chefs (invoice_prefix) WHERE status <> 'archived';

COMMENT ON TABLE chefs IS
    'Tenant root del SaaS HomeChef Pro. Cada registro es un chef inquilino. '
    'Pasada C / H-01..H-05. Default UUID 00000000-0000-0000-0000-000000000001 = piloto.';

-- ---------------------------------------------------------------------
-- Seed del chef piloto. UUID determinista para que tests, seeds y
-- migraciones futuras puedan referenciar el "piloto" sin conflictos.
-- ---------------------------------------------------------------------

-- Pasada C / H-03: el chef piloto ahora porta RIF + razon social SENIAT
-- + direccion fiscal en lugar de leerlos de appsettings.json.
-- Operador del SaaS edita estos valores con UPDATE antes del primer onboarding.
INSERT INTO chefs (
    id, rif, legal_name, trade_name, tax_address,
    timezone, base_currency, display_currency,
    invoice_prefix, contact_email, status, activated_at
) VALUES (
    '00000000-0000-0000-0000-000000000001',
    'J-12345678-9',
    'Cocina HCP, C.A.',
    'HomeChef Pro',
    'Av Principal, Caracas',
    'America/Caracas',
    'USD',
    'VES',
    'HC',
    'admin@homechef.local',
    'active',
    now()
) ON CONFLICT (id) DO NOTHING;
