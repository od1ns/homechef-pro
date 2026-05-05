-- =====================================================================
-- HomeChef Pro - Reviews de platos
-- =====================================================================
-- Reglas:
--  * Solo usuarios registrados pueden dejar review (clientes invitados no).
--  * El usuario debe tener un pedido 'delivered' que contenga el plato.
--    (Regla validada en capa Application, además de FK a order_id aquí).
--  * Una review por (usuario, pedido, plato).
--  * Admin puede ocultar (is_visible = FALSE) reviews abusivas.
-- =====================================================================

CREATE TABLE reviews (
    id              UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    chef_id                       UUID           NOT NULL REFERENCES chefs(id) DEFAULT '00000000-0000-0000-0000-000000000001',
    user_id         UUID           NOT NULL,                       -- AspNetUsers.Id
    order_id        UUID           NOT NULL REFERENCES orders(id),
    dish_id         UUID           NOT NULL REFERENCES recipes(id),

    rating          SMALLINT       NOT NULL,
    comment         TEXT,

    is_visible      BOOLEAN        NOT NULL DEFAULT TRUE,
    moderated_by    UUID,
    moderated_at    TIMESTAMPTZ,
    moderation_note TEXT,

    created_at      TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ    NOT NULL DEFAULT NOW(),

    CONSTRAINT review_rating_valid CHECK (rating BETWEEN 1 AND 5),
    UNIQUE (user_id, order_id, dish_id)
);

CREATE INDEX idx_reviews_dish    ON reviews(dish_id)    WHERE is_visible = TRUE;
CREATE INDEX idx_reviews_user    ON reviews(user_id);
CREATE INDEX idx_reviews_order   ON reviews(order_id);
CREATE INDEX idx_reviews_rating  ON reviews(rating);
CREATE INDEX idx_reviews_created ON reviews(created_at DESC);

COMMENT ON TABLE reviews IS 'Reviews de platos por clientes registrados con pedidos entregados.';
