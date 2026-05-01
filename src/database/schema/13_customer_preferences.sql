-- =====================================================================
-- HomeChef Pro - Preferencias del cliente
-- =====================================================================
-- Las apps móviles guardan localmente (shared_preferences) las respuestas
-- del onboarding (dietary, allergens, favoriteCategories, defaultAddress,
-- wantsLoyaltyUpdates). Cuando el cliente inicia sesión, esa data se
-- promueve al backend para que persista entre dispositivos. El blob es
-- JSONB freeform — la versión va dentro del objeto y se ignoran campos
-- desconocidos del lado del cliente, así que el cambio de schema es
-- forward-compatible sin migraciones.
-- =====================================================================

CREATE TABLE customer_preferences (
    user_id        UUID         PRIMARY KEY,            -- == AspNetUsers.Id
    payload        JSONB        NOT NULL DEFAULT '{}'::jsonb,
    updated_at     TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_customer_preferences_updated ON customer_preferences(updated_at DESC);

CREATE TRIGGER trg_customer_preferences_touch
    BEFORE UPDATE ON customer_preferences
    FOR EACH ROW EXECUTE FUNCTION fn_touch_updated_at();

COMMENT ON TABLE  customer_preferences         IS 'Preferencias del cliente (onboarding) sincronizadas desde la app móvil';
COMMENT ON COLUMN customer_preferences.payload IS 'JSON: { dietary[], allergens[], favoriteCategories[], defaultAddress, wantsLoyaltyUpdates, version }';
