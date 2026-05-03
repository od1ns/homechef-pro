-- =====================================================================
-- HomeChef Pro - Ejecutor agregado del esquema
-- =====================================================================
-- Uso: psql -U homechef -d homechef -f 99_run_all.sql
-- Asume que las tablas de Identity (AspNetUsers, AspNetRoles, ...)
-- ya fueron creadas por las migraciones de EF Core.
-- =====================================================================

\echo '>>> 00_extensions.sql';
\ir 00_extensions.sql

\echo '>>> 01_identity_notes.sql';
\ir 01_identity_notes.sql

\echo '>>> 01a_identity_tables.sql';
\ir 01a_identity_tables.sql

\echo '>>> 02_catalog_ingredients.sql';
\ir 02_catalog_ingredients.sql

\echo '>>> 03_catalog_recipes.sql';
\ir 03_catalog_recipes.sql

\echo '>>> 04_inventory_movements.sql';
\ir 04_inventory_movements.sql

\echo '>>> 05_exchange_rates.sql';
\ir 05_exchange_rates.sql

\echo '>>> 06_orders.sql';
\ir 06_orders.sql

\echo '>>> 07_payments.sql';
\ir 07_payments.sql

\echo '>>> 08_delivery.sql';
\ir 08_delivery.sql

\echo '>>> 09_reviews.sql';
\ir 09_reviews.sql

\echo '>>> 10_views.sql';
\ir 10_views.sql

\echo '>>> 11_functions_triggers.sql';
\ir 11_functions_triggers.sql

\echo '>>> 12_invoices.sql';
\ir 12_invoices.sql

\echo '>>> 13_customer_preferences.sql';
\ir 13_customer_preferences.sql

\echo '>>> 14_refresh_tokens.sql';
\ir 14_refresh_tokens.sql

\echo '>>> 15_inventory_rotation.sql';
\ir 15_inventory_rotation.sql

\echo '>>> 16_peak_hours.sql';
\ir 16_peak_hours.sql

\echo '>>> 17_customer_ranking.sql';
\ir 17_customer_ranking.sql

\echo 'Schema ready.';
