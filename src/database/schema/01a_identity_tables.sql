-- =====================================================================
-- HomeChef Pro - Tablas de ASP.NET Core Identity (snake_case)
-- =====================================================================
-- El DbContext es IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
-- y la convencion de naming convierte todas las tablas/columnas a snake_case.
-- Estas son las 7 tablas estandar de Identity con PK de tipo uuid.
-- =====================================================================

-- ---------------------------------------------------------------------
-- asp_net_roles
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS asp_net_roles (
    id                 uuid         PRIMARY KEY,
    name               varchar(256),
    normalized_name    varchar(256),
    concurrency_stamp  text
);

CREATE UNIQUE INDEX IF NOT EXISTS role_name_index
    ON asp_net_roles (normalized_name)
    WHERE normalized_name IS NOT NULL;

-- ---------------------------------------------------------------------
-- asp_net_users
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS asp_net_users (
    id                       uuid           PRIMARY KEY,
    user_name                varchar(256),
    normalized_user_name     varchar(256),
    email                    varchar(256),
    normalized_email         varchar(256),
    email_confirmed          boolean        NOT NULL DEFAULT false,
    password_hash            text,
    security_stamp           text,
    concurrency_stamp        text,
    phone_number             text,
    phone_number_confirmed   boolean        NOT NULL DEFAULT false,
    two_factor_enabled       boolean        NOT NULL DEFAULT false,
    lockout_end              timestamptz,
    lockout_enabled          boolean        NOT NULL DEFAULT false,
    access_failed_count      integer        NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX IF NOT EXISTS user_name_index
    ON asp_net_users (normalized_user_name)
    WHERE normalized_user_name IS NOT NULL;

CREATE INDEX IF NOT EXISTS email_index
    ON asp_net_users (normalized_email);

-- ---------------------------------------------------------------------
-- asp_net_role_claims
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS asp_net_role_claims (
    id           serial   PRIMARY KEY,
    role_id      uuid     NOT NULL REFERENCES asp_net_roles(id) ON DELETE CASCADE,
    claim_type   text,
    claim_value  text
);

CREATE INDEX IF NOT EXISTS ix_asp_net_role_claims_role_id
    ON asp_net_role_claims (role_id);

-- ---------------------------------------------------------------------
-- asp_net_user_claims
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS asp_net_user_claims (
    id           serial   PRIMARY KEY,
    user_id      uuid     NOT NULL REFERENCES asp_net_users(id) ON DELETE CASCADE,
    claim_type   text,
    claim_value  text
);

CREATE INDEX IF NOT EXISTS ix_asp_net_user_claims_user_id
    ON asp_net_user_claims (user_id);

-- ---------------------------------------------------------------------
-- asp_net_user_logins
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS asp_net_user_logins (
    login_provider          varchar(128),
    provider_key            varchar(128),
    provider_display_name   text,
    user_id                 uuid          NOT NULL REFERENCES asp_net_users(id) ON DELETE CASCADE,
    PRIMARY KEY (login_provider, provider_key)
);

CREATE INDEX IF NOT EXISTS ix_asp_net_user_logins_user_id
    ON asp_net_user_logins (user_id);

-- ---------------------------------------------------------------------
-- asp_net_user_roles
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS asp_net_user_roles (
    user_id   uuid   NOT NULL REFERENCES asp_net_users(id) ON DELETE CASCADE,
    role_id   uuid   NOT NULL REFERENCES asp_net_roles(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, role_id)
);

CREATE INDEX IF NOT EXISTS ix_asp_net_user_roles_role_id
    ON asp_net_user_roles (role_id);

-- ---------------------------------------------------------------------
-- asp_net_user_tokens
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS asp_net_user_tokens (
    user_id          uuid         NOT NULL REFERENCES asp_net_users(id) ON DELETE CASCADE,
    login_provider   varchar(128),
    name             varchar(128),
    value            text,
    PRIMARY KEY (user_id, login_provider, name)
);

-- ---------------------------------------------------------------------
-- Tabla de history de migraciones (esperada por EF Core).
-- Si en el futuro se introducen migraciones, EF la mantiene; por ahora
-- solo la creamos vacia para que el OnModelCreating no se queje.
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS __ef_migrations_history (
    migration_id      varchar(150)  PRIMARY KEY,
    product_version   varchar(32)   NOT NULL
);
