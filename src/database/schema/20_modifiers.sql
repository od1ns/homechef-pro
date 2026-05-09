-- =====================================================================
-- HomeChef Pro - Etapa 2: Modificadores de receta y snapshots en pedido
-- =====================================================================
-- Un modificador es una opcion que el cliente puede ajustar al pedir un
-- plato: "Sin cebolla", "Extra queso", "Aguacate +1". El chef los declara
-- desde admin_web. El cliente los ajusta en el bottom-sheet antes de
-- agregar al carrito.
--
-- Invariantes:
--   * min_qty >= 0, max_qty >= min_qty, default_qty entre [min,max].
--   * price_delta_usd puede ser negativo (quitar un ingrediente = descuento)
--     o cero (modificacion sin costo, ej. "sin sal").
--   * order_item_modifiers es inmutable: snapshots del nombre y precio al
--     momento de pagar, igual que dish_name_snapshot en order_items.
-- =====================================================================

CREATE TABLE recipe_modifiers (
    id               UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    chef_id          UUID           NOT NULL REFERENCES chefs(id)   DEFAULT '00000000-0000-0000-0000-000000000001',
    recipe_id        UUID           NOT NULL REFERENCES recipes(id) ON DELETE CASCADE,

    name             VARCHAR(200)   NOT NULL,          -- "Sin cebolla", "Extra queso"
    default_qty      INT            NOT NULL DEFAULT 0, -- cantidad pre-seleccionada (0 = ninguna)
    min_qty          INT            NOT NULL DEFAULT 0, -- minimo permitido
    max_qty          INT            NOT NULL DEFAULT 1, -- maximo permitido
    price_delta_usd  NUMERIC(12,4)  NOT NULL DEFAULT 0, -- surcharge (+) o descuento (-)

    display_order    INT            NOT NULL DEFAULT 0,
    is_active        BOOLEAN        NOT NULL DEFAULT TRUE,

    created_at       TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ    NOT NULL DEFAULT NOW(),

    CONSTRAINT modifier_name_nonempty    CHECK (char_length(trim(name)) > 0),
    CONSTRAINT modifier_qty_range        CHECK (min_qty >= 0 AND max_qty >= min_qty),
    CONSTRAINT modifier_default_in_range CHECK (default_qty >= min_qty AND default_qty <= max_qty)
);

CREATE INDEX idx_recipe_modifiers_recipe   ON recipe_modifiers(recipe_id);
CREATE INDEX idx_recipe_modifiers_active   ON recipe_modifiers(recipe_id, is_active) WHERE is_active = TRUE;

COMMENT ON TABLE  recipe_modifiers                IS 'Opciones de personalizacion del chef para cada plato';
COMMENT ON COLUMN recipe_modifiers.default_qty    IS 'Cantidad pre-seleccionada al mostrar el bottom-sheet al cliente';
COMMENT ON COLUMN recipe_modifiers.price_delta_usd IS 'Delta de precio por unidad del modificador en USD';

-- =====================================================================
-- Snapshots de modificadores seleccionados al momento del pedido
-- =====================================================================

CREATE TABLE order_item_modifiers (
    id                       UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    order_item_id            UUID          NOT NULL REFERENCES order_items(id) ON DELETE CASCADE,
    modifier_id              UUID          NOT NULL REFERENCES recipe_modifiers(id),

    -- Snapshots inmutables al momento del pedido
    modifier_name_snapshot   VARCHAR(200)  NOT NULL,
    quantity                 INT           NOT NULL,
    price_delta_usd_snapshot NUMERIC(12,4) NOT NULL,
    line_delta_usd           NUMERIC(12,4) NOT NULL, -- quantity * price_delta_usd_snapshot

    CONSTRAINT oim_qty_nonneg    CHECK (quantity >= 0),
    CONSTRAINT oim_line_computed CHECK (
        line_delta_usd = round(quantity::numeric * price_delta_usd_snapshot, 4)
    )
);

CREATE INDEX idx_oim_order_item ON order_item_modifiers(order_item_id);

COMMENT ON TABLE  order_item_modifiers                   IS 'Modificadores seleccionados por el cliente, inmutables post-pedido';
COMMENT ON COLUMN order_item_modifiers.line_delta_usd    IS 'quantity * price_delta_usd_snapshot, precalculado para reportes';
