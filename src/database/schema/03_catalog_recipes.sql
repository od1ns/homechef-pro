-- =====================================================================
-- HomeChef Pro - Recetas (platos finales + sub-recetas) y composición
-- =====================================================================
-- Una sola tabla 'recipes' alberga tanto platos finales (Pasticho) como
-- sub-recetas intermedias (Salsa de tomate, Base de bechamel, etc.).
-- Una sub-receta puede ser usada por múltiples platos o por otras sub-recetas.
--
-- La relación plato <-> ingrediente / sub-receta es N:M y recursiva, en la
-- tabla 'recipe_components'.
--
-- Invariantes:
--   * Sub-receta (is_sub_recipe = TRUE) DEBE declarar yield_quantity + yield_unit
--     (cuánto rinde al preparar la receta una vez).
--   * Plato final (is_sub_recipe = FALSE) DEBE tener selling_price_usd.
--   * Un componente apunta a UN ingrediente O a UNA sub-receta, nunca ambos.
--   * No se permiten ciclos: A no puede contener B si B contiene A
--     (verificado por trigger).
-- =====================================================================

CREATE TABLE recipes (
    id                       UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    chef_id                       UUID           NOT NULL REFERENCES chefs(id) DEFAULT '00000000-0000-0000-0000-000000000001',
    name                     VARCHAR(200)   NOT NULL,
    description              TEXT,
    category                 VARCHAR(60),          -- 'main','side','drink','dessert','sauce','base','other'
    is_sub_recipe            BOOLEAN        NOT NULL DEFAULT FALSE,

    -- Procedimiento paso a paso (markdown permitido). Visible en tablet de cocina.
    procedure_markdown       TEXT,

    -- Rendimiento (solo sub-recetas): cuánto produce 1 preparación de esta receta
    yield_quantity           NUMERIC(12,4),        -- ej. 500 (ml) ó 10 (porciones)
    yield_unit               VARCHAR(10),          -- 'g' | 'ml' | 'portion' | 'unit'

    -- Precio (solo platos finales)
    suggested_price_usd      NUMERIC(12,4),        -- sugerido por calculadora (markup)
    selling_price_usd        NUMERIC(12,4),        -- precio real de venta

    -- Tiempo de preparación por plato (minutos). Usado para ETA y cola de cocina.
    prep_time_minutes        INT            NOT NULL DEFAULT 0,

    image_url                TEXT,

    -- Estado operacional
    is_active                BOOLEAN        NOT NULL DEFAULT TRUE,
    is_out_of_stock          BOOLEAN        NOT NULL DEFAULT FALSE,   -- admin marca "agotado hoy"

    -- Menú mixto: fijos siempre visibles + especiales del día
    menu_type                VARCHAR(20)    NOT NULL DEFAULT 'fixed', -- 'fixed' | 'daily_special'
    special_from             TIMESTAMPTZ,
    special_to               TIMESTAMPTZ,

    created_at               TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    updated_at               TIMESTAMPTZ    NOT NULL DEFAULT NOW(),

    CONSTRAINT recipe_menu_type_valid    CHECK (menu_type IN ('fixed', 'daily_special')),
    CONSTRAINT recipe_yield_unit_valid   CHECK (
        yield_unit IS NULL OR yield_unit IN ('g', 'ml', 'portion', 'unit')
    ),
    CONSTRAINT recipe_sub_has_yield CHECK (
        (is_sub_recipe = FALSE) OR (yield_quantity IS NOT NULL AND yield_unit IS NOT NULL AND yield_quantity > 0)
    ),
    CONSTRAINT recipe_dish_has_price CHECK (
        (is_sub_recipe = TRUE) OR (selling_price_usd IS NOT NULL AND selling_price_usd > 0)
    ),
    CONSTRAINT recipe_prep_time_nonneg   CHECK (prep_time_minutes >= 0),
    CONSTRAINT recipe_special_window     CHECK (
        menu_type <> 'daily_special'
        OR (special_from IS NOT NULL AND special_to IS NOT NULL AND special_from < special_to)
    )
);

CREATE INDEX idx_recipes_active        ON recipes(is_active) WHERE is_active = TRUE;
CREATE INDEX idx_recipes_subrecipe     ON recipes(is_sub_recipe);
CREATE INDEX idx_recipes_menu_type     ON recipes(menu_type);
CREATE INDEX idx_recipes_category      ON recipes(category);
CREATE INDEX idx_recipes_name_trgm     ON recipes USING gin (name gin_trgm_ops);
CREATE INDEX idx_recipes_special_window ON recipes(special_from, special_to)
    WHERE menu_type = 'daily_special';

COMMENT ON TABLE  recipes                  IS 'Platos finales y sub-recetas unificados';
COMMENT ON COLUMN recipes.is_sub_recipe    IS 'TRUE = componente reutilizable; FALSE = plato vendible';
COMMENT ON COLUMN recipes.yield_quantity   IS 'Rendimiento en yield_unit al preparar 1 vez la receta (solo sub-recetas)';
COMMENT ON COLUMN recipes.is_out_of_stock  IS 'Flag manual del admin: agotado por hoy, sigue siendo visible como "no disponible"';

-- =====================================================================
-- Composición: relaciona una receta padre con sus componentes
-- (ingredientes directos O sub-recetas)
-- =====================================================================

CREATE TABLE recipe_components (
    id                   UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    chef_id                       UUID           NOT NULL REFERENCES chefs(id) DEFAULT '00000000-0000-0000-0000-000000000001',
    parent_recipe_id     UUID           NOT NULL REFERENCES recipes(id) ON DELETE CASCADE,

    -- Exactamente UNO de los dos debe ser NOT NULL
    ingredient_id        UUID           REFERENCES ingredients(id) ON DELETE RESTRICT,
    sub_recipe_id        UUID           REFERENCES recipes(id)     ON DELETE RESTRICT,

    -- Cantidad requerida:
    --   si ingredient_id: en la use_unit del ingrediente
    --   si sub_recipe_id: en la yield_unit de esa sub-receta
    quantity             NUMERIC(14,4)  NOT NULL,
    notes                VARCHAR(200),
    display_order        INT            NOT NULL DEFAULT 0,
    created_at           TIMESTAMPTZ    NOT NULL DEFAULT NOW(),

    CONSTRAINT component_exactly_one CHECK (
        (ingredient_id IS NOT NULL AND sub_recipe_id IS NULL)
        OR (ingredient_id IS NULL AND sub_recipe_id IS NOT NULL)
    ),
    CONSTRAINT component_no_self_ref CHECK (parent_recipe_id <> sub_recipe_id),
    CONSTRAINT component_qty_positive CHECK (quantity > 0)
);

CREATE INDEX idx_components_parent     ON recipe_components(parent_recipe_id);
CREATE INDEX idx_components_ingredient ON recipe_components(ingredient_id) WHERE ingredient_id IS NOT NULL;
CREATE INDEX idx_components_subrecipe  ON recipe_components(sub_recipe_id) WHERE sub_recipe_id IS NOT NULL;

-- Impide duplicados exactos (mismo padre + mismo hijo)
CREATE UNIQUE INDEX uq_components_parent_ingredient
    ON recipe_components(parent_recipe_id, ingredient_id)
    WHERE ingredient_id IS NOT NULL;
CREATE UNIQUE INDEX uq_components_parent_subrecipe
    ON recipe_components(parent_recipe_id, sub_recipe_id)
    WHERE sub_recipe_id IS NOT NULL;

COMMENT ON TABLE recipe_components IS 'Aristas del DAG de recetas: conecta padre con sus ingredientes o sub-recetas';
