-- =====================================================================
-- HomeChef Pro - Codigos de invitacion (Sesion A / Frente 1)
-- =====================================================================
-- Restringe el registro publico: solo clientes con un codigo valido pueden
-- crear cuenta. El admin del SaaS o el chef pueden generar codigos.
--
-- Modelo C (cross-chef + per-chef):
--   - chef_id NULL: codigo global generado por admin del SaaS. Vale para
--     que cualquiera se registre como Client.
--   - chef_id NOT NULL: generado por un chef o admin del SaaS para
--     rastrear que chef invito al cliente. No impone restriccion dura
--     (cliente puede ser de varios chefs en el modelo cross-tenant) pero
--     queda en metadata para reportes.
--
-- Ciclo de vida:
--   - active: revoked_at IS NULL AND (expires_at IS NULL OR expires_at > now())
--             AND used_count < max_uses
--   - exhausted: used_count >= max_uses
--   - expired: expires_at < now()
--   - revoked: revoked_at IS NOT NULL
-- =====================================================================

CREATE TABLE invitation_codes (
    id                  UUID         PRIMARY KEY DEFAULT gen_random_uuid(),

    -- El string que el cliente pega en el formulario de registro.
    -- 12 chars alfanumericos legibles (sin 0/O/I/l). Generamos en backend.
    code                VARCHAR(32)  NOT NULL UNIQUE,

    -- Si NULL: codigo global (admin SaaS). Si NOT NULL: rastrea chef invitante.
    chef_id             UUID         REFERENCES chefs(id) ON DELETE SET NULL,

    -- Quien lo creo (admin SaaS o admin de chef).
    created_by          UUID         NOT NULL REFERENCES asp_net_users(id),
    created_at          TIMESTAMPTZ  NOT NULL DEFAULT now(),

    -- Vigencia. NULL en expires_at = no expira. max_uses = 1 por default
    -- para invitaciones individuales; valores mayores para "campañas".
    expires_at          TIMESTAMPTZ,
    max_uses            INTEGER      NOT NULL DEFAULT 1 CHECK (max_uses > 0),
    used_count          INTEGER      NOT NULL DEFAULT 0 CHECK (used_count >= 0),

    -- Revocacion (admin puede invalidar antes de que se use).
    revoked_at          TIMESTAMPTZ,
    revoked_by          UUID         REFERENCES asp_net_users(id),
    revocation_reason   VARCHAR(200),

    -- Descripcion libre para el admin (ej. "Familia Lopez", "Campaña navidad").
    notes               VARCHAR(500),

    -- Constraint: used_count nunca puede exceder max_uses.
    CONSTRAINT chk_used_count_le_max CHECK (used_count <= max_uses)
);

-- Indices
CREATE INDEX idx_invitation_codes_chef_id
    ON invitation_codes (chef_id)
    WHERE chef_id IS NOT NULL;

CREATE INDEX idx_invitation_codes_created_by
    ON invitation_codes (created_by);

-- Index parcial: solo codigos activos (revoked_at NULL).
CREATE INDEX idx_invitation_codes_active
    ON invitation_codes (created_at DESC)
    WHERE revoked_at IS NULL;

COMMENT ON TABLE invitation_codes IS
    'Codigos de invitacion para restringir el registro publico. '
    'Sesion A / Frente 1. chef_id NULL = global SaaS; NOT NULL = rastrea chef invitante.';

-- ---------------------------------------------------------------------
-- Tabla de uso: registra cada vez que un codigo se uso (audit trail).
-- Permite saber quien se registro con qué codigo, cuando, desde donde.
-- ---------------------------------------------------------------------

CREATE TABLE invitation_code_uses (
    id                  UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    invitation_code_id  UUID         NOT NULL REFERENCES invitation_codes(id) ON DELETE CASCADE,
    used_by_user_id     UUID         NOT NULL REFERENCES asp_net_users(id) ON DELETE CASCADE,
    used_at             TIMESTAMPTZ  NOT NULL DEFAULT now(),
    user_ip             VARCHAR(45),  -- IPv4 o IPv6
    user_agent          VARCHAR(500),

    UNIQUE (invitation_code_id, used_by_user_id)  -- un user no puede usar el mismo codigo 2 veces
);

CREATE INDEX idx_invitation_code_uses_code
    ON invitation_code_uses (invitation_code_id, used_at DESC);
CREATE INDEX idx_invitation_code_uses_user
    ON invitation_code_uses (used_by_user_id);

COMMENT ON TABLE invitation_code_uses IS
    'Audit trail: cada vez que se usa un codigo de invitacion, se registra aqui. '
    'Sirve para reportes de "que chef genero mas conversiones".';
