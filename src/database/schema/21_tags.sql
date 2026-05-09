-- =====================================================================
-- HomeChef Pro - Etapa 3: Tags / badges de receta
-- =====================================================================
-- Almacena etiquetas de dieta y estilo del plato para filtrado y
-- visualizacion en el menú del cliente.
-- Valores permitidos: vegano, vegetariano, picante, sin_gluten,
--                     sin_lactosa, nuevo, popular.
-- =====================================================================

ALTER TABLE recipes
    ADD COLUMN IF NOT EXISTS tags text[] NOT NULL DEFAULT '{}';

-- Indice GIN para consultas de "contiene tag X" eficientes.
CREATE INDEX IF NOT EXISTS idx_recipes_tags ON recipes USING gin(tags);

COMMENT ON COLUMN recipes.tags IS 'Etiquetas del plato: vegano, vegetariano, picante, sin_gluten, sin_lactosa, nuevo, popular';
