CREATE EXTENSION IF NOT EXISTS pg_trgm;

ALTER TABLE products ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;

UPDATE products
SET publish_status = 'draft'
WHERE publish_status NOT IN ('draft', 'published');

ALTER TABLE products DROP CONSTRAINT IF EXISTS ck_products_publish_status;
ALTER TABLE products
    ADD CONSTRAINT ck_products_publish_status CHECK (publish_status IN ('draft', 'published'));

DROP INDEX IF EXISTS ix_products_public_listing;
CREATE INDEX IF NOT EXISTS ix_products_public_listing
    ON products (is_active, publish_status, availability_status, primary_category_id, sort_order);

CREATE INDEX IF NOT EXISTS ix_products_active_category
    ON products (is_active, primary_category_id, sort_order);

CREATE INDEX IF NOT EXISTS ix_products_name_trgm ON products USING gin (name gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_products_slug_trgm ON products USING gin (slug gin_trgm_ops);

CREATE TABLE homepage_sections (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code text NOT NULL,
    title text NOT NULL,
    type text NOT NULL,
    item_limit integer NOT NULL,
    sort_order integer NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_homepage_sections_code_not_blank CHECK (btrim(code) <> ''),
    CONSTRAINT ck_homepage_sections_title_not_blank CHECK (btrim(title) <> ''),
    CONSTRAINT ck_homepage_sections_type CHECK (type IN ('product_list', 'category_list')),
    CONSTRAINT ck_homepage_sections_item_limit_positive CHECK (item_limit > 0)
);

CREATE UNIQUE INDEX ux_homepage_sections_code ON homepage_sections (code);
CREATE INDEX ix_homepage_sections_active_sort_order ON homepage_sections (is_active, sort_order, code);

CREATE TABLE homepage_section_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    section_id uuid NOT NULL REFERENCES homepage_sections (id) ON DELETE CASCADE,
    product_id uuid NULL REFERENCES products (id) ON DELETE RESTRICT,
    category_id uuid NULL REFERENCES categories (id) ON DELETE RESTRICT,
    sort_order integer NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_homepage_section_items_single_target CHECK (num_nonnulls(product_id, category_id) = 1)
);

CREATE UNIQUE INDEX ux_homepage_section_items_section_product
    ON homepage_section_items (section_id, product_id)
    WHERE product_id IS NOT NULL;

CREATE UNIQUE INDEX ux_homepage_section_items_section_category
    ON homepage_section_items (section_id, category_id)
    WHERE category_id IS NOT NULL;

CREATE INDEX ix_homepage_section_items_section_sort_order
    ON homepage_section_items (section_id, is_active, sort_order, id);

CREATE OR REPLACE FUNCTION validate_homepage_section_item_target()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    section_type text;
BEGIN
    SELECT type INTO section_type
    FROM homepage_sections
    WHERE id = NEW.section_id;

    IF section_type = 'product_list' AND NEW.product_id IS NULL THEN
        RAISE EXCEPTION 'Homepage section % requires product item.', NEW.section_id
            USING ERRCODE = 'check_violation';
    END IF;

    IF section_type = 'category_list' AND NEW.category_id IS NULL THEN
        RAISE EXCEPTION 'Homepage section % requires category item.', NEW.section_id
            USING ERRCODE = 'check_violation';
    END IF;

    IF section_type = 'product_list' AND NEW.category_id IS NOT NULL THEN
        RAISE EXCEPTION 'Homepage product section % cannot use category item.', NEW.section_id
            USING ERRCODE = 'check_violation';
    END IF;

    IF section_type = 'category_list' AND NEW.product_id IS NOT NULL THEN
        RAISE EXCEPTION 'Homepage category section % cannot use product item.', NEW.section_id
            USING ERRCODE = 'check_violation';
    END IF;

    RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION validate_homepage_section_type_change()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.type = 'product_list' AND EXISTS (
        SELECT 1
        FROM homepage_section_items
        WHERE section_id = NEW.id
            AND category_id IS NOT NULL
    ) THEN
        RAISE EXCEPTION 'Homepage section % cannot change to product_list while category items exist.', NEW.id
            USING ERRCODE = 'check_violation';
    END IF;

    IF NEW.type = 'category_list' AND EXISTS (
        SELECT 1
        FROM homepage_section_items
        WHERE section_id = NEW.id
            AND product_id IS NOT NULL
    ) THEN
        RAISE EXCEPTION 'Homepage section % cannot change to category_list while product items exist.', NEW.id
            USING ERRCODE = 'check_violation';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_homepage_section_items_validate_target
BEFORE INSERT OR UPDATE OF section_id, product_id, category_id ON homepage_section_items
FOR EACH ROW EXECUTE FUNCTION validate_homepage_section_item_target();

CREATE TRIGGER trg_homepage_sections_validate_type_change
BEFORE UPDATE OF type ON homepage_sections
FOR EACH ROW EXECUTE FUNCTION validate_homepage_section_type_change();

CREATE TRIGGER trg_homepage_sections_set_updated_at
BEFORE UPDATE ON homepage_sections
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_homepage_section_items_set_updated_at
BEFORE UPDATE ON homepage_section_items
FOR EACH ROW EXECUTE FUNCTION set_updated_at();

INSERT INTO homepage_sections (code, title, type, item_limit, sort_order, is_active)
VALUES
    ('hero_products', 'Hero: ходовые позиции', 'product_list', 3, 10, TRUE),
    ('featured_products', 'Популярные позиции', 'product_list', 8, 20, TRUE),
    ('direction_categories', 'Направления', 'category_list', 4, 30, TRUE)
ON CONFLICT (code) DO UPDATE
SET
    title = EXCLUDED.title,
    type = EXCLUDED.type,
    item_limit = EXCLUDED.item_limit,
    sort_order = EXCLUDED.sort_order,
    is_active = EXCLUDED.is_active;
