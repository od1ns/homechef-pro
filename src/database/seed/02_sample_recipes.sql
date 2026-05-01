-- =====================================================================
-- HomeChef Pro - Seed: recetas de muestra (sub-recetas + platos finales)
-- =====================================================================
-- Demuestra la estructura recursiva:
--  * Salsa Bolognesa (sub-receta)  -> tomate, cebolla, ajo, carne
--  * Salsa Bechamel  (sub-receta)  -> harina, leche, mantequilla*
--  * Pasticho        (plato final) -> pasta, Bolognesa, Bechamel, queso
--  * Hallaca veggie  (plato final) -> harina pan, plátano, queso (simple, sin sub-receta)
-- =====================================================================

-- ---------------------------------------------------------------------
-- Sub-recetas
-- ---------------------------------------------------------------------
INSERT INTO recipes (id, name, description, category, is_sub_recipe, yield_quantity, yield_unit, prep_time_minutes, procedure_markdown)
VALUES
    (
      '22222222-0000-0000-0000-000000000001',
      'Salsa Bolognesa',
      'Salsa base de tomate con carne molida para pasticho y pastas',
      'sauce', TRUE, 1500, 'ml', 35,
      E'# Procedimiento\n\n1. Picar cebolla y ajo finamente.\n2. Sofreír en aceite 3 min.\n3. Agregar carne molida y dorar.\n4. Añadir tomate triturado y cocinar 20 min a fuego medio.\n5. Salpimentar y agregar orégano.'
    ),
    (
      '22222222-0000-0000-0000-000000000002',
      'Salsa Bechamel',
      'Salsa blanca a base de leche y harina',
      'sauce', TRUE, 1000, 'ml', 20,
      E'# Procedimiento\n\n1. Derretir aceite (o mantequilla) en olla.\n2. Agregar harina y revolver 1 min formando un roux.\n3. Incorporar leche poco a poco batiendo.\n4. Cocinar a fuego bajo 10 min hasta espesar.\n5. Salpimentar.'
    )
ON CONFLICT DO NOTHING;

-- Componentes de la Bolognesa (1500 ml rinde)
INSERT INTO recipe_components (parent_recipe_id, ingredient_id, quantity, display_order)
VALUES
    ('22222222-0000-0000-0000-000000000001', '11111111-0000-0000-0000-000000000001',  800, 1), -- 800 g tomate
    ('22222222-0000-0000-0000-000000000001', '11111111-0000-0000-0000-000000000002',  200, 2), -- 200 g cebolla
    ('22222222-0000-0000-0000-000000000001', '11111111-0000-0000-0000-000000000003',   20, 3), -- 20 g ajo
    ('22222222-0000-0000-0000-000000000001', '11111111-0000-0000-0000-000000000006',  500, 4), -- 500 g carne molida
    ('22222222-0000-0000-0000-000000000001', '11111111-0000-0000-0000-00000000000a',   40, 5), -- 40 ml aceite
    ('22222222-0000-0000-0000-000000000001', '11111111-0000-0000-0000-00000000000b',   10, 6), -- 10 g sal
    ('22222222-0000-0000-0000-000000000001', '11111111-0000-0000-0000-00000000000e',    5, 7), -- 5 g orégano
    ('22222222-0000-0000-0000-000000000001', '11111111-0000-0000-0000-00000000000f',    3, 8)  -- 3 g pimienta
ON CONFLICT DO NOTHING;

-- Componentes de la Bechamel (1000 ml rinde)
INSERT INTO recipe_components (parent_recipe_id, ingredient_id, quantity, display_order)
VALUES
    ('22222222-0000-0000-0000-000000000002', '11111111-0000-0000-0000-000000000004',  80,  1), -- 80 g harina
    ('22222222-0000-0000-0000-000000000002', '11111111-0000-0000-0000-00000000000c', 900,  2), -- 900 ml leche
    ('22222222-0000-0000-0000-000000000002', '11111111-0000-0000-0000-00000000000a',  60,  3), -- 60 ml aceite
    ('22222222-0000-0000-0000-000000000002', '11111111-0000-0000-0000-00000000000b',   5,  4)  -- 5 g sal
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------
-- Platos finales
-- ---------------------------------------------------------------------
INSERT INTO recipes (id, name, description, category, is_sub_recipe, selling_price_usd, suggested_price_usd, prep_time_minutes, menu_type, procedure_markdown)
VALUES
    (
      '33333333-0000-0000-0000-000000000001',
      'Pasticho',
      'Clásico pasticho casero con bolognesa, bechamel y queso gratinado',
      'main', FALSE, 9.00, 8.50, 45, 'fixed',
      E'# Montaje\n\n1. Hervir pasta al dente (8 min).\n2. En refractaria: capa de bolognesa, capa de pasta, capa de bechamel. Repetir 2 veces.\n3. Cubrir con queso mozzarella.\n4. Hornear 180°C por 25 min hasta gratinar.\n5. Reposar 10 min antes de servir.'
    ),
    (
      '33333333-0000-0000-0000-000000000002',
      'Arepa Reina Pepiada',
      'Arepa rellena de pollo mechado con aguacate (clásica criolla)',
      'main', FALSE, 5.50, 5.00, 20, 'fixed',
      E'# Procedimiento\n\n1. Hervir pollo con sal 25 min.\n2. Desmechar y mezclar con mayonesa y aguacate.\n3. Mezclar harina pan con agua y sal hasta obtener masa.\n4. Asar arepas en budare 4 min cada lado.\n5. Abrir y rellenar.'
    )
ON CONFLICT DO NOTHING;

-- Plato especial del día (incluye ventana special_from/special_to obligatoria)
INSERT INTO recipes (id, name, description, category, is_sub_recipe, selling_price_usd, suggested_price_usd, prep_time_minutes, menu_type, special_from, special_to, procedure_markdown)
VALUES
    (
      '33333333-0000-0000-0000-000000000003',
      'Pabellón Criollo',
      'Carne mechada, caraotas negras, arroz blanco y tajadas',
      'main', FALSE, 8.00, 7.50, 40, 'daily_special',
      CURRENT_DATE, CURRENT_DATE + INTERVAL '7 days',
      E'# Procedimiento\n\n1. Cocinar caraotas con cebolla y ajo (requieren remojo previo de 12 h).\n2. Mechar carne previamente cocida con sofrito.\n3. Cocinar arroz blanco.\n4. Freír tajadas de plátano maduro.\n5. Montar en plato: carne, caraotas, arroz, tajadas.'
    )
ON CONFLICT DO NOTHING;

-- Componentes Pasticho (1 porción)
INSERT INTO recipe_components (parent_recipe_id, ingredient_id, sub_recipe_id, quantity, display_order, notes)
VALUES
    ('33333333-0000-0000-0000-000000000001', '11111111-0000-0000-0000-000000000013', NULL, 120, 1, 'Pasta larga hervida'),
    ('33333333-0000-0000-0000-000000000001', NULL, '22222222-0000-0000-0000-000000000001', 200, 2, 'Bolognesa en ml'),
    ('33333333-0000-0000-0000-000000000001', NULL, '22222222-0000-0000-0000-000000000002', 150, 3, 'Bechamel en ml'),
    ('33333333-0000-0000-0000-000000000001', '11111111-0000-0000-0000-000000000008', NULL,  80, 4, 'Mozzarella para gratinar')
ON CONFLICT DO NOTHING;

-- Componentes Arepa Reina Pepiada (1 unidad)
INSERT INTO recipe_components (parent_recipe_id, ingredient_id, quantity, display_order, notes)
VALUES
    ('33333333-0000-0000-0000-000000000002', '11111111-0000-0000-0000-000000000005', 120, 1, 'Harina pan'),
    ('33333333-0000-0000-0000-000000000002', '11111111-0000-0000-0000-000000000007', 100, 2, 'Pollo mechado'),
    ('33333333-0000-0000-0000-000000000002', '11111111-0000-0000-0000-00000000000b',   3, 3, 'Sal')
ON CONFLICT DO NOTHING;

-- Componentes Pabellón Criollo (1 porción)
INSERT INTO recipe_components (parent_recipe_id, ingredient_id, quantity, display_order, notes)
VALUES
    ('33333333-0000-0000-0000-000000000003', '11111111-0000-0000-0000-000000000006', 150, 1, 'Carne mechada (puede usar molida)'),
    ('33333333-0000-0000-0000-000000000003', '11111111-0000-0000-0000-000000000012', 120, 2, 'Caraotas negras'),
    ('33333333-0000-0000-0000-000000000003', '11111111-0000-0000-0000-000000000011', 100, 3, 'Arroz blanco'),
    ('33333333-0000-0000-0000-000000000003', '11111111-0000-0000-0000-000000000014',   1, 4, 'Plátano para tajadas'),
    ('33333333-0000-0000-0000-000000000003', '11111111-0000-0000-0000-00000000000a',  20, 5, 'Aceite para freír'),
    ('33333333-0000-0000-0000-000000000003', '11111111-0000-0000-0000-00000000000b',   5, 6, 'Sal')
ON CONFLICT DO NOTHING;
