-- =====================================================================
-- HomeChef Pro - Extensiones de PostgreSQL
-- =====================================================================
-- Habilita funcionalidades nativas necesarias para el esquema:
--   pgcrypto    -> gen_random_uuid()       (IDs UUID v4)
--   citext      -> comparaciones case-insensitive (emails, nombres opc.)
--   pg_trgm     -> búsqueda parcial rápida en nombres de platos/ingr.
--   btree_gin   -> índices compuestos para filtros de menú
-- Requiere PostgreSQL 13+
-- =====================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS citext;
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS btree_gin;
