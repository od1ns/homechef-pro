-- Etapa 1: agregar imageUrl a los 3 platos seed para demo Internal Testing.
-- URLs de Wikimedia Commons via Special:FilePath (redirige al thumb real).
-- Verificadas con curl 200 OK el 2026-05-08.

-- UPDATE incondicional para corregir URLs previas que estaban rotas (404).
UPDATE recipes
SET image_url = 'https://commons.wikimedia.org/wiki/Special:FilePath/Pabell%C3%B3n_Criollo_Venezolano_1.jpg?width=800'
WHERE name = 'Pabellón Criollo';

UPDATE recipes
SET image_url = 'https://commons.wikimedia.org/wiki/Special:FilePath/Arepa-reina-pepiada-arepasdelgringo.jpg?width=800'
WHERE name = 'Arepa Reina Pepiada';

UPDATE recipes
SET image_url = 'https://commons.wikimedia.org/wiki/Special:FilePath/Un_pasticho_venezolano_propio_mio.jpg?width=800'
WHERE name = 'Pasticho';

SELECT name, image_url FROM recipes WHERE is_sub_recipe = false;
