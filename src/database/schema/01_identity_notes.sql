-- =====================================================================
-- HomeChef Pro - Notas sobre tablas de identidad (ASP.NET Core Identity)
-- =====================================================================
-- Las tablas de autenticación NO se crean aquí.
-- Las genera ASP.NET Core Identity vía migraciones de EF Core.
--
-- Tablas que EF Core creará (IdentityUser<Guid>):
--   AspNetUsers          (id UUID, email, password_hash, phone, etc.)
--   AspNetRoles          (id UUID, name)        -- Admin, Cashier, Cook, Client
--   AspNetUserRoles      (user_id, role_id)
--   AspNetUserClaims     (...)
--   AspNetUserLogins     (...)
--   AspNetUserTokens     (...)
--   AspNetRoleClaims     (...)
--
-- Todas las columnas del dominio que referencian a usuarios usan:
--   user_id  UUID  -- apunta a AspNetUsers(Id)
--   Sin FK formal a nivel DB (para evitar dependencia circular de migraciones),
--   la integridad se valida en capa de Application.
--
-- Perfil extendido del usuario (campos que no existen en AspNetUsers):
-- =====================================================================

CREATE TABLE user_profiles (
    user_id              UUID           PRIMARY KEY,  -- == AspNetUsers.Id
    full_name            VARCHAR(160)   NOT NULL,
    default_phone        VARCHAR(30),
    default_address      TEXT,
    preferred_language   VARCHAR(10)    NOT NULL DEFAULT 'es-VE',
    avatar_url           TEXT,
    created_at           TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    updated_at           TIMESTAMPTZ    NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE  user_profiles            IS 'Datos de perfil extendido, 1-1 con AspNetUsers';
COMMENT ON COLUMN user_profiles.user_id    IS 'FK lógica a AspNetUsers.Id (sin constraint DB por orden de migración)';
