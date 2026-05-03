-- =====================================================================
-- HomeChef Pro - Programa de fidelidad "Sabor"
-- =====================================================================
-- Modelo de puntos por compra:
--   * 1 punto por cada USD entero gastado en orders.status = 'delivered'.
--   * Solo aplica a clientes registered (orders.user_id NOT NULL).
--     Las órdenes guest NO acreditan (no hay forma de identificarlos).
--   * Niveles segun lifetime_earned:
--        bronce: 0-499 puntos acumulados
--        plata:  500-999
--        oro:    1000+
--   * Las recompensas tienen un costo en puntos y se canjean restando del
--     balance actual. El canje queda asentado en loyalty_transactions.
-- =====================================================================

-- ---------------------------------------------------------------------
-- Cuentas de fidelidad: una por usuario registrado.
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS loyalty_accounts (
    user_id           uuid          PRIMARY KEY REFERENCES asp_net_users(id) ON DELETE CASCADE,
    current_balance   integer       NOT NULL DEFAULT 0 CHECK (current_balance >= 0),
    lifetime_earned   integer       NOT NULL DEFAULT 0 CHECK (lifetime_earned >= 0),
    level             varchar(10)   NOT NULL DEFAULT 'bronce'
                                    CHECK (level IN ('bronce', 'plata', 'oro')),
    created_at        timestamptz   NOT NULL DEFAULT now(),
    updated_at        timestamptz   NOT NULL DEFAULT now()
);

COMMENT ON TABLE loyalty_accounts IS 'Saldo y nivel del programa Sabor por usuario.';

-- ---------------------------------------------------------------------
-- Movimientos: cada acreditación o canje. Permite reconstruir el balance.
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS loyalty_transactions (
    id                  uuid          PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id             uuid          NOT NULL REFERENCES asp_net_users(id) ON DELETE CASCADE,
    type                varchar(10)   NOT NULL CHECK (type IN ('earn', 'redeem', 'adjust')),
    points              integer       NOT NULL,  -- positivo siempre; el tipo define si suma o resta
    related_order_id    uuid          REFERENCES orders(id) ON DELETE SET NULL,
    related_reward_id   uuid          ,           -- FK suelto al catalogo de rewards
    notes               text,
    created_at          timestamptz   NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_loyalty_transactions_user_id
    ON loyalty_transactions (user_id, created_at DESC);

COMMENT ON TABLE loyalty_transactions IS 'Movimientos del programa Sabor (acreditaciones por compra y canjes).';

-- ---------------------------------------------------------------------
-- Catálogo de recompensas configurables.
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS loyalty_rewards (
    id              uuid          PRIMARY KEY DEFAULT gen_random_uuid(),
    name            varchar(120)  NOT NULL,
    description     text,
    cost_points     integer       NOT NULL CHECK (cost_points > 0),
    reward_type     varchar(20)   NOT NULL CHECK (reward_type IN
                                    ('free_dessert','free_delivery','discount_pct','free_dinner')),
    reward_value    text,                       -- JSON opcional con detalles
    is_active       boolean       NOT NULL DEFAULT true,
    created_at      timestamptz   NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_loyalty_rewards_active
    ON loyalty_rewards (is_active) WHERE is_active = TRUE;

COMMENT ON TABLE loyalty_rewards IS 'Catalogo de recompensas canjeables por puntos Sabor.';

-- ---------------------------------------------------------------------
-- Trigger de acreditación: cuando una orden pasa a 'delivered' y tiene
-- user_id (registered customer), acreditamos puntos = floor(total_usd).
-- Idempotente: revisa si ya existe una transaction earn para esa orden.
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_credit_loyalty_on_delivery()
RETURNS TRIGGER AS $$
DECLARE
    v_points integer;
    v_new_lifetime integer;
    v_new_level varchar(10);
BEGIN
    -- Solo cuando recién pasamos a delivered, no en re-saves.
    IF NEW.status <> 'delivered' OR OLD.status = 'delivered' THEN
        RETURN NEW;
    END IF;

    -- Solo registered customers.
    IF NEW.user_id IS NULL THEN
        RETURN NEW;
    END IF;

    -- Idempotencia: si ya hay un earn para esta orden, no re-acreditar.
    IF EXISTS (
        SELECT 1 FROM loyalty_transactions
        WHERE related_order_id = NEW.id AND type = 'earn'
    ) THEN
        RETURN NEW;
    END IF;

    v_points := FLOOR(NEW.total_usd)::integer;
    IF v_points <= 0 THEN
        RETURN NEW;
    END IF;

    -- Asegurar que la cuenta exista.
    INSERT INTO loyalty_accounts (user_id)
    VALUES (NEW.user_id)
    ON CONFLICT (user_id) DO NOTHING;

    -- Calcular el nuevo lifetime para determinar level.
    SELECT lifetime_earned + v_points INTO v_new_lifetime
    FROM loyalty_accounts WHERE user_id = NEW.user_id;

    v_new_level := CASE
        WHEN v_new_lifetime >= 1000 THEN 'oro'
        WHEN v_new_lifetime >= 500  THEN 'plata'
        ELSE 'bronce'
    END;

    -- Actualizar saldo + lifetime + level.
    UPDATE loyalty_accounts
    SET current_balance = current_balance + v_points,
        lifetime_earned = v_new_lifetime,
        level           = v_new_level,
        updated_at      = now()
    WHERE user_id = NEW.user_id;

    -- Asentar el movimiento.
    INSERT INTO loyalty_transactions (user_id, type, points, related_order_id, notes)
    VALUES (NEW.user_id, 'earn', v_points, NEW.id,
            'Earn por orden ' || NEW.order_number);

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_orders_loyalty_credit ON orders;
CREATE TRIGGER trg_orders_loyalty_credit
    AFTER UPDATE OF status ON orders
    FOR EACH ROW
    EXECUTE FUNCTION fn_credit_loyalty_on_delivery();

-- ---------------------------------------------------------------------
-- Seed de recompensas estándar.
-- ---------------------------------------------------------------------
INSERT INTO loyalty_rewards (id, name, description, cost_points, reward_type, reward_value)
VALUES
    ('99999999-0000-0000-0000-000000000001',
     'Postre gratis',
     'Cualquier postre del menu, sin costo.',
     100, 'free_dessert', NULL),
    ('99999999-0000-0000-0000-000000000002',
     'Envio gratis',
     'Tu proximo envio sin cargo.',
     250, 'free_delivery', NULL),
    ('99999999-0000-0000-0000-000000000003',
     '25% de descuento',
     'Descuento del 25% en tu proxima orden.',
     500, 'discount_pct', '{"percentage":25}'),
    ('99999999-0000-0000-0000-000000000004',
     'Cena para 2',
     'Cena completa para dos personas, hasta USD 30.',
     1000, 'free_dinner', '{"max_value_usd":30}')
ON CONFLICT (id) DO NOTHING;
