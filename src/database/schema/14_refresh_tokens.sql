-- =====================================================================
-- HomeChef Pro - Tokens de refresh (rotacion segura del JWT)
-- =====================================================================
-- Cada login emite un access token (JWT, vida corta ~60 min) y un
-- refresh token (vida larga ~14 dias) que se intercambia por nuevos
-- pares cuando expira el access. El backend solo guarda el HASH del
-- token, nunca el token plano (defensa en profundidad por si el dump
-- de la BD se filtra).
--
-- Politica de rotacion:
--   - Cada uso del refresh token EMITE uno nuevo y REVOCA el anterior.
--   - Si alguien intenta usar uno ya revocado, se asume robo y se
--     revoca toda la cadena de ese usuario (replaced_by_id permite
--     reconstruirla).
-- =====================================================================

CREATE TABLE IF NOT EXISTS refresh_tokens (
    id              uuid          PRIMARY KEY,
    user_id         uuid          NOT NULL REFERENCES asp_net_users(id) ON DELETE CASCADE,

    -- Hash SHA-256 del token plano (en hex). Indice unico para que
    -- la verificacion sea O(log n) sin guardar el token original.
    token_hash      varchar(64)   NOT NULL UNIQUE,

    -- Vida util del token. Despues de expires_at deja de ser aceptado
    -- aunque no este revocado.
    expires_at      timestamptz   NOT NULL,
    created_at      timestamptz   NOT NULL DEFAULT now(),

    -- Si el token fue rotado, replaced_by_id apunta al token nuevo.
    -- Sirve para reconstruir cadenas y detectar reuso (token revocado
    -- que vuelve a aparecer en uso).
    revoked_at      timestamptz,
    replaced_by_id  uuid          REFERENCES refresh_tokens(id) ON DELETE SET NULL,

    -- Metadata de auditoria (opcional, util para detectar uso anomalo).
    device_info     varchar(500),
    ip_address      varchar(45)   -- IPv6 alcanza con 45 chars
);

CREATE INDEX IF NOT EXISTS ix_refresh_tokens_user_id
    ON refresh_tokens (user_id);

CREATE INDEX IF NOT EXISTS ix_refresh_tokens_expires_at
    ON refresh_tokens (expires_at)
    WHERE revoked_at IS NULL;

COMMENT ON TABLE refresh_tokens IS
'Refresh tokens para rotacion segura del JWT. El backend solo guarda el hash, nunca el token plano.';
