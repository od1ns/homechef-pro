-- =====================================================================
-- HomeChef Pro - Ingredientes y presentaciones de compra
-- =====================================================================
-- Modelo:
--  * Cada ingrediente tiene UNA unidad canónica de uso (gramos, ml o unidad)
--    que es la granularidad mínima con que se mide en las recetas.
--  * Un mismo ingrediente puede comprarse en MÚLTIPLES presentaciones
--    (ej. tomate: "Kg suelto", "Caja 10 kg", "Unidad"). Cada presentación
--    declara su factor de conversión hacia la unidad de uso.
--  * Stock y costo promedio ponderado se llevan en la unidad de uso.
-- =====================================================================

CREATE TABLE ingredients (
    id                            UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    chef_id                       UUID           NOT NULL REFERENCES chefs(id) DEFAULT '00000000-0000-0000-0000-000000000001',
    -- Pasada C / H-02: name UNIQUE compuesto con chef_id (cada chef puede tener
    -- su propio "Tomate"). Hoy con un solo chef equivale a UNIQUE global.
    name                          VARCHAR(120)   NOT NULL,
    description                   TEXT,

    -- Unidad canónica de uso en recetas
    use_unit                      VARCHAR(10)    NOT NULL,  -- 'g' | 'ml' | 'unit'

    -- Stock actual en unidad de uso
    current_stock_use_unit        NUMERIC(14,4)  NOT NULL DEFAULT 0,

    -- Gatillos de reabastecimiento (en unidad de uso)
    reorder_point_use_unit        NUMERIC(14,4)  NOT NULL DEFAULT 0,
    minimum_stock_use_unit        NUMERIC(14,4)  NOT NULL DEFAULT 0,

    -- Costo promedio ponderado por unidad de uso (actualizado en cada compra)
    avg_cost_per_use_unit_usd     NUMERIC(14,6)  NOT NULL DEFAULT 0,

    is_active                     BOOLEAN        NOT NULL DEFAULT TRUE,
    created_at                    TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    updated_at                    TIMESTAMPTZ    NOT NULL DEFAULT NOW(),

    CONSTRAINT ingredient_use_unit_valid    CHECK (use_unit IN ('g', 'ml', 'unit')),
    CONSTRAINT ingredient_stock_nonneg      CHECK (current_stock_use_unit >= 0),
    CONSTRAINT ingredient_reorder_nonneg    CHECK (reorder_point_use_unit >= 0),
    CONSTRAINT ingredient_min_nonneg        CHECK (minimum_stock_use_unit >= 0),
    CONSTRAINT ingredient_cost_nonneg       CHECK (avg_cost_per_use_unit_usd >= 0),

    UNIQUE (chef_id, name)
);

CREATE INDEX idx_ingredients_active ON ingredients(is_active) WHERE is_active = TRUE;
CREATE INDEX idx_ingredients_name_trgm ON ingredients USING gin (name gin_trgm_ops);

COMMENT ON TABLE  ingredients                              IS 'Catálogo maestro de insumos (materias primas)';
COMMENT ON COLUMN ingredients.use_unit                     IS 'Unidad canónica de uso: g | ml | unit';
COMMENT ON COLUMN ingredients.avg_cost_per_use_unit_usd    IS 'Costo promedio ponderado por unidad de uso (actualizado por trigger on purchase)';

-- =====================================================================
-- Presentaciones de compra
-- =====================================================================
-- Cada compra se hace en UNA presentación específica de un ingrediente.
-- La conversión a unidad de uso se hace con conversion_to_use_unit.
--
-- Ejemplo: ingrediente "Harina", use_unit='g'
--   Presentación A: "Saco 50 kg", purchase_unit='kg', purchase_quantity=50,
--                   conversion_to_use_unit=1000   (1 kg = 1000 g)
--   Presentación B: "Bolsa 1 kg", purchase_unit='kg', purchase_quantity=1,
--                   conversion_to_use_unit=1000
--   Presentación C: "Unidad",     purchase_unit='unit', purchase_quantity=1,
--                   conversion_to_use_unit=500    (1 unidad = 500 g)
-- =====================================================================

CREATE TABLE ingredient_presentations (
    id                         UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    chef_id                       UUID           NOT NULL REFERENCES chefs(id) DEFAULT '00000000-0000-0000-0000-000000000001',
    ingredient_id              UUID           NOT NULL REFERENCES ingredients(id) ON DELETE RESTRICT,

    name                       VARCHAR(120)   NOT NULL,   -- "Saco 50 kg", "Caja 12 u."
    purchase_unit              VARCHAR(10)    NOT NULL,   -- unidad física de compra
    purchase_quantity          NUMERIC(10,4)  NOT NULL,   -- cuánto trae esa presentación
    conversion_to_use_unit     NUMERIC(14,6)  NOT NULL,   -- 1 purchase_unit => X use_units

    last_purchase_price_usd    NUMERIC(12,4),             -- precio USD por 1 presentación (snapshot)
    is_active                  BOOLEAN        NOT NULL DEFAULT TRUE,
    created_at                 TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    updated_at                 TIMESTAMPTZ    NOT NULL DEFAULT NOW(),

    CONSTRAINT presentation_purchase_unit_valid CHECK (
        purchase_unit IN ('kg', 'g', 'l', 'ml', 'unit', 'box', 'sack', 'bag', 'bottle', 'pack')
    ),
    CONSTRAINT presentation_qty_positive        CHECK (purchase_quantity > 0),
    CONSTRAINT presentation_conversion_positive CHECK (conversion_to_use_unit > 0),
    CONSTRAINT presentation_price_nonneg        CHECK (last_purchase_price_usd IS NULL OR last_purchase_price_usd >= 0),
    UNIQUE (ingredient_id, name)
);

CREATE INDEX idx_presentations_ingredient ON ingredient_presentations(ingredient_id);
CREATE INDEX idx_presentations_active      ON ingredient_presentations(is_active) WHERE is_active = TRUE;

COMMENT ON TABLE  ingredient_presentations                        IS 'Formas en que se compra un ingrediente (múltiples por ingrediente)';
COMMENT ON COLUMN ingredient_presentations.conversion_to_use_unit IS 'Multiplicador: (purchase_quantity * conversion) => cantidad en use_unit del ingrediente';
