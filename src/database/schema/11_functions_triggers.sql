-- =====================================================================
-- HomeChef Pro - Funciones y triggers
-- =====================================================================
-- Reglas de negocio mantenidas a nivel DB (invariantes fuertes):
--   * Detección de ciclos en recipe_components (DAG de recetas)
--   * Actualización de stock + costo promedio al comprar un ingrediente
--   * Decremento de stock al registrar una merma
--   * Generación del order_number (HC-YYYYMMDD-NNNN)
--   * Actualización de updated_at en tablas con ese campo
-- =====================================================================

-- ---------------------------------------------------------------------
-- Mantener updated_at al día
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_touch_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at := NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_user_profiles_touch
    BEFORE UPDATE ON user_profiles
    FOR EACH ROW EXECUTE FUNCTION fn_touch_updated_at();

CREATE TRIGGER trg_ingredients_touch
    BEFORE UPDATE ON ingredients
    FOR EACH ROW EXECUTE FUNCTION fn_touch_updated_at();

CREATE TRIGGER trg_presentations_touch
    BEFORE UPDATE ON ingredient_presentations
    FOR EACH ROW EXECUTE FUNCTION fn_touch_updated_at();

CREATE TRIGGER trg_recipes_touch
    BEFORE UPDATE ON recipes
    FOR EACH ROW EXECUTE FUNCTION fn_touch_updated_at();

CREATE TRIGGER trg_orders_touch
    BEFORE UPDATE ON orders
    FOR EACH ROW EXECUTE FUNCTION fn_touch_updated_at();

CREATE TRIGGER trg_delivery_tracking_touch
    BEFORE UPDATE ON delivery_tracking
    FOR EACH ROW EXECUTE FUNCTION fn_touch_updated_at();

CREATE TRIGGER trg_reviews_touch
    BEFORE UPDATE ON reviews
    FOR EACH ROW EXECUTE FUNCTION fn_touch_updated_at();

-- ---------------------------------------------------------------------
-- Detección de ciclos en recipe_components
-- ---------------------------------------------------------------------
-- Antes de insertar/actualizar una arista parent->sub_recipe, verificar
-- que NO exista un camino desde sub_recipe hasta parent (lo cual crearía
-- un ciclo). Rechaza la operación con error claro.
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_check_recipe_no_cycle()
RETURNS TRIGGER AS $$
DECLARE
    v_found INT;
BEGIN
    IF NEW.sub_recipe_id IS NULL THEN
        RETURN NEW;  -- componentes ingredientes nunca crean ciclos
    END IF;

    -- Caso obvio (ya cubierto por CHECK pero lo reforzamos)
    IF NEW.parent_recipe_id = NEW.sub_recipe_id THEN
        RAISE EXCEPTION 'Una receta no puede contenerse a sí misma (parent=sub=%)', NEW.parent_recipe_id
            USING ERRCODE = '23514';
    END IF;

    -- ¿Existe un camino desde NEW.sub_recipe_id hasta NEW.parent_recipe_id?
    WITH RECURSIVE reachable AS (
        SELECT sub_recipe_id AS node_id, 1 AS depth
        FROM   recipe_components
        WHERE  parent_recipe_id = NEW.sub_recipe_id
          AND  sub_recipe_id IS NOT NULL

        UNION ALL

        SELECT rc.sub_recipe_id, r.depth + 1
        FROM   recipe_components rc
        JOIN   reachable r ON rc.parent_recipe_id = r.node_id
        WHERE  rc.sub_recipe_id IS NOT NULL
          AND  r.depth < 10
    )
    SELECT 1 INTO v_found
    FROM   reachable
    WHERE  node_id = NEW.parent_recipe_id
    LIMIT  1;

    IF v_found IS NOT NULL THEN
        RAISE EXCEPTION 'Ciclo detectado en recetas: % ya depende (transitivamente) de %',
            NEW.sub_recipe_id, NEW.parent_recipe_id
            USING ERRCODE = '23514';
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_recipe_components_no_cycle
    BEFORE INSERT OR UPDATE ON recipe_components
    FOR EACH ROW EXECUTE FUNCTION fn_check_recipe_no_cycle();

-- ---------------------------------------------------------------------
-- Actualización de stock y costo promedio al comprar ingrediente
-- ---------------------------------------------------------------------
-- Fórmula de costo promedio ponderado:
--   new_avg = (old_stock * old_avg + added_stock * added_cost_per_unit)
--             / (old_stock + added_stock)
--
-- Donde:
--   added_stock       = quantity_purchased * purchase_quantity * conversion_to_use_unit
--   added_cost_per_unit = total_cost_usd / added_stock
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_apply_purchase_to_stock()
RETURNS TRIGGER AS $$
DECLARE
    v_pres               ingredient_presentations%ROWTYPE;
    v_old_stock          NUMERIC(14,4);
    v_old_avg            NUMERIC(14,6);
    v_added_use_units    NUMERIC(14,4);
    v_added_cost_per_u   NUMERIC(14,6);
    v_new_stock          NUMERIC(14,4);
    v_new_avg            NUMERIC(14,6);
BEGIN
    SELECT * INTO v_pres
    FROM ingredient_presentations
    WHERE id = NEW.presentation_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Presentación % no existe', NEW.presentation_id;
    END IF;

    IF v_pres.ingredient_id <> NEW.ingredient_id THEN
        RAISE EXCEPTION 'La presentación % no corresponde al ingrediente %',
            NEW.presentation_id, NEW.ingredient_id;
    END IF;

    -- Convertir presentaciones compradas a unidades de uso
    v_added_use_units := NEW.quantity_purchased
                        * v_pres.purchase_quantity
                        * v_pres.conversion_to_use_unit;

    IF v_added_use_units <= 0 THEN
        RAISE EXCEPTION 'La compra resulta en 0 unidades de uso (revisar conversion_to_use_unit)';
    END IF;

    v_added_cost_per_u := NEW.total_cost_usd / v_added_use_units;

    SELECT current_stock_use_unit, avg_cost_per_use_unit_usd
      INTO v_old_stock, v_old_avg
    FROM ingredients
    WHERE id = NEW.ingredient_id
    FOR UPDATE;

    v_new_stock := v_old_stock + v_added_use_units;

    IF v_new_stock = 0 THEN
        v_new_avg := v_added_cost_per_u;
    ELSE
        v_new_avg := ((v_old_stock * v_old_avg) + (v_added_use_units * v_added_cost_per_u))
                     / v_new_stock;
    END IF;

    UPDATE ingredients
       SET current_stock_use_unit    = v_new_stock,
           avg_cost_per_use_unit_usd = v_new_avg,
           updated_at                = NOW()
     WHERE id = NEW.ingredient_id;

    -- Refrescar el último precio conocido de la presentación
    UPDATE ingredient_presentations
       SET last_purchase_price_usd = NEW.unit_price_usd,
           updated_at              = NOW()
     WHERE id = NEW.presentation_id;

    -- Auditoría
    INSERT INTO inventory_movements (
        ingredient_id, movement_type, quantity_use_unit, cost_impact_usd,
        source_table, source_id, resulting_stock, resulting_avg_cost, occurred_at, notes
    ) VALUES (
        NEW.ingredient_id, 'purchase', v_added_use_units, NEW.total_cost_usd,
        'ingredient_purchases', NEW.id, v_new_stock, v_new_avg, NEW.purchased_at, NEW.notes
    );

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_ingredient_purchase_apply
    AFTER INSERT ON ingredient_purchases
    FOR EACH ROW EXECUTE FUNCTION fn_apply_purchase_to_stock();

-- ---------------------------------------------------------------------
-- Decremento de stock al registrar merma
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_apply_waste_to_stock()
RETURNS TRIGGER AS $$
DECLARE
    v_old_stock  NUMERIC(14,4);
    v_old_avg    NUMERIC(14,6);
    v_new_stock  NUMERIC(14,4);
BEGIN
    SELECT current_stock_use_unit, avg_cost_per_use_unit_usd
      INTO v_old_stock, v_old_avg
    FROM ingredients
    WHERE id = NEW.ingredient_id
    FOR UPDATE;

    IF NEW.quantity_use_unit > v_old_stock THEN
        RAISE EXCEPTION 'Merma de % excede el stock actual (%) del ingrediente %',
            NEW.quantity_use_unit, v_old_stock, NEW.ingredient_id
            USING ERRCODE = '23514';
    END IF;

    v_new_stock := v_old_stock - NEW.quantity_use_unit;

    -- Alinear estimated_cost_usd con costo promedio actual si vino en 0
    IF NEW.estimated_cost_usd = 0 THEN
        NEW.estimated_cost_usd := NEW.quantity_use_unit * v_old_avg;
    END IF;

    UPDATE ingredients
       SET current_stock_use_unit = v_new_stock,
           updated_at              = NOW()
     WHERE id = NEW.ingredient_id;

    INSERT INTO inventory_movements (
        ingredient_id, movement_type, quantity_use_unit, cost_impact_usd,
        source_table, source_id, resulting_stock, resulting_avg_cost, occurred_at, notes
    ) VALUES (
        NEW.ingredient_id, 'waste', -NEW.quantity_use_unit, NEW.estimated_cost_usd,
        'ingredient_waste', NEW.id, v_new_stock, v_old_avg, NEW.recorded_at,
        CONCAT('reason=', NEW.reason)
    );

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_ingredient_waste_apply
    BEFORE INSERT ON ingredient_waste
    FOR EACH ROW EXECUTE FUNCTION fn_apply_waste_to_stock();

-- ---------------------------------------------------------------------
-- Generación del order_number correlativo diario
-- ---------------------------------------------------------------------
-- Formato: HC-YYYYMMDD-NNNN (ej. HC-20260423-0001)
-- Usamos una secuencia auxiliar por día via tabla counter simple.
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS order_number_counters (
    -- Pasada C / H-02: correlativo per-chef. PK compuesto (chef_id, for_date).
    -- Default piloto preserva comportamiento actual.
    chef_id        UUID    NOT NULL REFERENCES chefs(id) DEFAULT '00000000-0000-0000-0000-000000000001',
    for_date       DATE    NOT NULL,
    last_sequence  INT     NOT NULL DEFAULT 0,
    PRIMARY KEY (chef_id, for_date)
);

CREATE OR REPLACE FUNCTION fn_generate_order_number()
RETURNS TRIGGER AS $$
DECLARE
    -- Pasada C / H-02: correlativo per-chef.
    -- Toma invoice_prefix y timezone del chef para que cada inquilino tenga
    -- su propia secuencia HC-YYYYMMDD-NNNN (o CHB-..., etc).
    v_prefix   VARCHAR(4);
    v_timezone VARCHAR(50);
    v_today    DATE;
    v_seq      INT;
BEGIN
    IF NEW.order_number IS NOT NULL AND NEW.order_number <> '' THEN
        RETURN NEW;  -- respetar si ya vino asignado
    END IF;

    -- Lookup del chef: prefix y zona horaria. Si por alguna razon NEW.chef_id
    -- no existe, defaulteamos a HC + Caracas para no romper INSERTs.
    SELECT invoice_prefix, timezone
      INTO v_prefix, v_timezone
      FROM chefs
     WHERE id = NEW.chef_id;

    IF v_prefix IS NULL THEN
        v_prefix := 'HC';
    END IF;
    IF v_timezone IS NULL THEN
        v_timezone := 'America/Caracas';
    END IF;

    v_today := (NEW.created_at AT TIME ZONE v_timezone)::date;

    INSERT INTO order_number_counters (chef_id, for_date, last_sequence)
    VALUES (NEW.chef_id, v_today, 1)
    ON CONFLICT (chef_id, for_date)
    DO UPDATE SET last_sequence = order_number_counters.last_sequence + 1
    RETURNING last_sequence INTO v_seq;

    NEW.order_number := v_prefix || '-' || to_char(v_today, 'YYYYMMDD') || '-' || lpad(v_seq::text, 4, '0');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_orders_order_number
    BEFORE INSERT ON orders
    FOR EACH ROW EXECUTE FUNCTION fn_generate_order_number();

-- =====================================================================
-- F-24 (audit Pasada B): regenerar access_token si llega vacio o NULL
-- =====================================================================
-- EF Core con ValueGeneratedOnAdd envia la columna en el INSERT con valor "" cuando
-- la propiedad CLR es string vacio. El DEFAULT del schema solo aplica cuando la columna
-- NO se menciona en el INSERT, asi que sin este trigger todos los orders terminaban
-- con access_token = "" violando el UNIQUE constraint.
-- Mismo patron que fn_generate_order_number.
-- =====================================================================
CREATE OR REPLACE FUNCTION fn_generate_access_token()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.access_token IS NULL OR NEW.access_token = '' THEN
        NEW.access_token := encode(gen_random_bytes(24), 'hex');
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_orders_access_token
    BEFORE INSERT ON orders
    FOR EACH ROW EXECUTE FUNCTION fn_generate_access_token();
