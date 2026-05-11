CREATE TABLE stored_files (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    storage_key text NOT NULL,
    original_file_name text NOT NULL,
    content_type text NOT NULL,
    size_bytes bigint NOT NULL,
    checksum text NOT NULL,
    purpose text NOT NULL,
    status text NOT NULL DEFAULT 'active',
    created_by_user_id uuid NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_stored_files_storage_key_not_blank CHECK (btrim(storage_key) <> ''),
    CONSTRAINT ck_stored_files_original_file_name_not_blank CHECK (btrim(original_file_name) <> ''),
    CONSTRAINT ck_stored_files_content_type_not_blank CHECK (btrim(content_type) <> ''),
    CONSTRAINT ck_stored_files_size_bytes_non_negative CHECK (size_bytes >= 0),
    CONSTRAINT ck_stored_files_checksum_not_blank CHECK (btrim(checksum) <> ''),
    CONSTRAINT ck_stored_files_purpose CHECK (purpose IN ('product_image', 'brand_logo', 'import_source', 'export_result', 'temp')),
    CONSTRAINT ck_stored_files_status CHECK (status IN ('active', 'deleted', 'orphaned'))
);

CREATE UNIQUE INDEX ux_stored_files_storage_key ON stored_files (storage_key);
CREATE INDEX ix_stored_files_status_created_at ON stored_files (status, created_at);

CREATE TABLE categories (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    parent_id uuid NULL REFERENCES categories (id) ON DELETE RESTRICT,
    name text NOT NULL,
    slug text NOT NULL,
    description text NULL,
    seo_title text NULL,
    seo_description text NULL,
    h1 text NULL,
    sort_order integer NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    is_visible_in_menu boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_categories_name_not_blank CHECK (btrim(name) <> ''),
    CONSTRAINT ck_categories_slug_not_blank CHECK (btrim(slug) <> ''),
    CONSTRAINT ck_categories_not_own_parent CHECK (parent_id IS NULL OR parent_id <> id)
);

CREATE UNIQUE INDEX ux_categories_slug ON categories (slug);
CREATE INDEX ix_categories_parent_id_sort_order ON categories (parent_id, sort_order);
CREATE INDEX ix_categories_active_menu ON categories (is_active, is_visible_in_menu, sort_order);

CREATE TABLE brands (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name text NOT NULL,
    slug text NOT NULL,
    description text NULL,
    seo_title text NULL,
    seo_description text NULL,
    logo_file_id uuid NULL REFERENCES stored_files (id) ON DELETE SET NULL,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_brands_name_not_blank CHECK (btrim(name) <> ''),
    CONSTRAINT ck_brands_slug_not_blank CHECK (btrim(slug) <> '')
);

CREATE UNIQUE INDEX ux_brands_slug ON brands (slug);
CREATE INDEX ix_brands_active_name ON brands (is_active, name);

CREATE TABLE products (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    primary_category_id uuid NOT NULL REFERENCES categories (id) ON DELETE RESTRICT,
    name text NOT NULL,
    slug text NOT NULL,
    sku text NULL,
    external_id text NULL,
    brand_id uuid NULL REFERENCES brands (id) ON DELETE SET NULL,
    description text NULL,
    short_description text NULL,
    availability_status text NOT NULL DEFAULT 'check_availability',
    sale_unit text NOT NULL,
    unit_quantity text NOT NULL,
    publish_status text NOT NULL DEFAULT 'draft',
    seo_title text NULL,
    seo_description text NULL,
    h1 text NULL,
    sort_order integer NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_products_name_not_blank CHECK (btrim(name) <> ''),
    CONSTRAINT ck_products_slug_not_blank CHECK (btrim(slug) <> ''),
    CONSTRAINT ck_products_sku_not_blank CHECK (sku IS NULL OR btrim(sku) <> ''),
    CONSTRAINT ck_products_external_id_not_blank CHECK (external_id IS NULL OR btrim(external_id) <> ''),
    CONSTRAINT ck_products_availability_status CHECK (availability_status IN ('in_stock', 'on_order', 'check_availability')),
    CONSTRAINT ck_products_sale_unit CHECK (sale_unit IN ('coil', 'box', 'piece', 'pack')),
    CONSTRAINT ck_products_unit_quantity_not_blank CHECK (btrim(unit_quantity) <> ''),
    CONSTRAINT ck_products_publish_status CHECK (publish_status IN ('draft', 'published', 'archived'))
);

CREATE UNIQUE INDEX ux_products_slug ON products (slug);
CREATE UNIQUE INDEX ux_products_sku ON products (sku) WHERE sku IS NOT NULL;
CREATE UNIQUE INDEX ux_products_external_id ON products (external_id) WHERE external_id IS NOT NULL;
CREATE INDEX ix_products_primary_category_id_sort_order ON products (primary_category_id, sort_order);
CREATE INDEX ix_products_brand_id ON products (brand_id);
CREATE INDEX ix_products_public_listing ON products (publish_status, availability_status, primary_category_id, sort_order);

CREATE TABLE product_images (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id uuid NOT NULL REFERENCES products (id) ON DELETE CASCADE,
    stored_file_id uuid NOT NULL REFERENCES stored_files (id) ON DELETE RESTRICT,
    alt text NOT NULL,
    title text NULL,
    sort_order integer NOT NULL DEFAULT 0,
    is_main boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_product_images_alt_not_blank CHECK (btrim(alt) <> '')
);

CREATE UNIQUE INDEX ux_product_images_stored_file_id ON product_images (stored_file_id);
CREATE UNIQUE INDEX ux_product_images_single_main ON product_images (product_id) WHERE is_main;
CREATE INDEX ix_product_images_product_id_sort_order ON product_images (product_id, sort_order);

CREATE TABLE category_attributes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    category_id uuid NOT NULL REFERENCES categories (id) ON DELETE CASCADE,
    name text NOT NULL,
    code text NOT NULL,
    type text NOT NULL,
    unit text NULL,
    is_required boolean NOT NULL DEFAULT false,
    is_filterable boolean NOT NULL DEFAULT false,
    is_comparable boolean NOT NULL DEFAULT false,
    is_visible_in_product boolean NOT NULL DEFAULT true,
    is_seo_important boolean NOT NULL DEFAULT false,
    is_used_in_generated_name boolean NOT NULL DEFAULT false,
    sort_order integer NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_category_attributes_name_not_blank CHECK (btrim(name) <> ''),
    CONSTRAINT ck_category_attributes_code_not_blank CHECK (btrim(code) <> ''),
    CONSTRAINT ck_category_attributes_type CHECK (type IN ('text', 'number', 'select', 'boolean')),
    CONSTRAINT ck_category_attributes_unit_not_blank CHECK (unit IS NULL OR btrim(unit) <> '')
);

CREATE UNIQUE INDEX ux_category_attributes_category_id_code ON category_attributes (category_id, code);
CREATE INDEX ix_category_attributes_category_id_sort_order ON category_attributes (category_id, sort_order);
CREATE INDEX ix_category_attributes_filterable ON category_attributes (category_id, is_filterable, sort_order);
CREATE INDEX ix_category_attributes_comparable ON category_attributes (category_id, is_comparable, sort_order);

CREATE TABLE attribute_options (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    attribute_id uuid NOT NULL REFERENCES category_attributes (id) ON DELETE CASCADE,
    value text NOT NULL,
    slug text NOT NULL,
    normalized_value text NOT NULL,
    sort_order integer NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_attribute_options_value_not_blank CHECK (btrim(value) <> ''),
    CONSTRAINT ck_attribute_options_slug_not_blank CHECK (btrim(slug) <> ''),
    CONSTRAINT ck_attribute_options_normalized_value_not_blank CHECK (btrim(normalized_value) <> '')
);

CREATE UNIQUE INDEX ux_attribute_options_attribute_id_slug ON attribute_options (attribute_id, slug);
CREATE UNIQUE INDEX ux_attribute_options_attribute_id_normalized_value ON attribute_options (attribute_id, normalized_value);
CREATE INDEX ix_attribute_options_attribute_id_sort_order ON attribute_options (attribute_id, sort_order);

CREATE TABLE attribute_value_aliases (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    attribute_id uuid NOT NULL REFERENCES category_attributes (id) ON DELETE CASCADE,
    option_id uuid NOT NULL REFERENCES attribute_options (id) ON DELETE CASCADE,
    alias text NOT NULL,
    normalized_alias text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_attribute_value_aliases_alias_not_blank CHECK (btrim(alias) <> ''),
    CONSTRAINT ck_attribute_value_aliases_normalized_alias_not_blank CHECK (btrim(normalized_alias) <> '')
);

CREATE UNIQUE INDEX ux_attribute_value_aliases_attribute_id_normalized_alias
    ON attribute_value_aliases (attribute_id, normalized_alias);
CREATE INDEX ix_attribute_value_aliases_option_id ON attribute_value_aliases (option_id);

CREATE TABLE product_attribute_values (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id uuid NOT NULL REFERENCES products (id) ON DELETE CASCADE,
    attribute_id uuid NOT NULL REFERENCES category_attributes (id) ON DELETE RESTRICT,
    value_text text NULL,
    value_number numeric NULL,
    value_boolean boolean NULL,
    attribute_option_id uuid NULL REFERENCES attribute_options (id) ON DELETE RESTRICT,
    normalized_value text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_product_attribute_values_single_storage_column CHECK (
        num_nonnulls(value_text, value_number, value_boolean, attribute_option_id) = 1
    ),
    CONSTRAINT ck_product_attribute_values_text_not_blank CHECK (value_text IS NULL OR btrim(value_text) <> ''),
    CONSTRAINT ck_product_attribute_values_normalized_not_blank CHECK (normalized_value IS NULL OR btrim(normalized_value) <> '')
);

CREATE UNIQUE INDEX ux_product_attribute_values_product_id_attribute_id
    ON product_attribute_values (product_id, attribute_id);
CREATE INDEX ix_product_attribute_values_product_id ON product_attribute_values (product_id);
CREATE INDEX ix_product_attribute_values_attribute_id ON product_attribute_values (attribute_id);
CREATE INDEX ix_product_attribute_values_attribute_option_id ON product_attribute_values (attribute_option_id);
CREATE INDEX ix_product_attribute_values_attribute_id_value_number
    ON product_attribute_values (attribute_id, value_number)
    WHERE value_number IS NOT NULL;
CREATE INDEX ix_product_attribute_values_attribute_id_normalized_value
    ON product_attribute_values (attribute_id, normalized_value)
    WHERE normalized_value IS NOT NULL;

CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$;

CREATE OR REPLACE FUNCTION validate_category_parent_cycle()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.parent_id IS NULL THEN
        RETURN NEW;
    END IF;

    IF EXISTS (
        WITH RECURSIVE category_ancestors AS (
            SELECT id, parent_id
            FROM categories
            WHERE id = NEW.parent_id

            UNION ALL

            SELECT parent.id, parent.parent_id
            FROM categories parent
            INNER JOIN category_ancestors child ON child.parent_id = parent.id
        )
        SELECT 1
        FROM category_ancestors
        WHERE id = NEW.id
    ) THEN
        RAISE EXCEPTION 'Category % cannot use its descendant % as parent.', NEW.id, NEW.parent_id
            USING ERRCODE = 'check_violation';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_categories_validate_parent_cycle
BEFORE INSERT OR UPDATE OF parent_id ON categories
FOR EACH ROW
EXECUTE FUNCTION validate_category_parent_cycle();

CREATE TRIGGER trg_categories_set_updated_at
BEFORE UPDATE ON categories
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_brands_set_updated_at
BEFORE UPDATE ON brands
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_products_set_updated_at
BEFORE UPDATE ON products
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

CREATE OR REPLACE FUNCTION validate_product_primary_category_change()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.primary_category_id = OLD.primary_category_id THEN
        RETURN NEW;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM product_attribute_values
        WHERE product_id = NEW.id
    ) THEN
        RAISE EXCEPTION 'Product % primary category cannot be changed while attribute values exist.', NEW.id
            USING ERRCODE = 'check_violation';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_products_validate_primary_category_change
BEFORE UPDATE OF primary_category_id ON products
FOR EACH ROW
EXECUTE FUNCTION validate_product_primary_category_change();

CREATE OR REPLACE FUNCTION validate_brand_logo_file()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    file_purpose text;
BEGIN
    IF NEW.logo_file_id IS NULL THEN
        RETURN NEW;
    END IF;

    SELECT purpose
    INTO file_purpose
    FROM stored_files
    WHERE id = NEW.logo_file_id;

    IF file_purpose <> 'brand_logo' THEN
        RAISE EXCEPTION 'Stored file % cannot be used as a brand logo.', NEW.logo_file_id
            USING ERRCODE = 'check_violation';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_brands_validate_logo_file
BEFORE INSERT OR UPDATE OF logo_file_id ON brands
FOR EACH ROW
EXECUTE FUNCTION validate_brand_logo_file();

CREATE OR REPLACE FUNCTION validate_product_image_file()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    file_purpose text;
BEGIN
    SELECT purpose
    INTO file_purpose
    FROM stored_files
    WHERE id = NEW.stored_file_id;

    IF file_purpose <> 'product_image' THEN
        RAISE EXCEPTION 'Stored file % cannot be used as a product image.', NEW.stored_file_id
            USING ERRCODE = 'check_violation';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_product_images_validate_file
BEFORE INSERT OR UPDATE OF stored_file_id ON product_images
FOR EACH ROW
EXECUTE FUNCTION validate_product_image_file();

CREATE TRIGGER trg_product_images_set_updated_at
BEFORE UPDATE ON product_images
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_category_attributes_set_updated_at
BEFORE UPDATE ON category_attributes
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_attribute_options_set_updated_at
BEFORE UPDATE ON attribute_options
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_attribute_value_aliases_set_updated_at
BEFORE UPDATE ON attribute_value_aliases
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_product_attribute_values_set_updated_at
BEFORE UPDATE ON product_attribute_values
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

CREATE OR REPLACE FUNCTION validate_attribute_option_attribute()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    option_attribute_id uuid;
BEGIN
    SELECT attribute_id
    INTO option_attribute_id
    FROM attribute_options
    WHERE id = NEW.option_id;

    IF option_attribute_id IS NULL OR option_attribute_id <> NEW.attribute_id THEN
        RAISE EXCEPTION 'Attribute option % does not belong to attribute %.', NEW.option_id, NEW.attribute_id
            USING ERRCODE = 'check_violation';
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_attribute_value_aliases_validate_option
BEFORE INSERT OR UPDATE ON attribute_value_aliases
FOR EACH ROW
EXECUTE FUNCTION validate_attribute_option_attribute();

CREATE OR REPLACE FUNCTION validate_product_attribute_value()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    attribute_type text;
    attribute_category_id uuid;
    product_category_id uuid;
    option_attribute_id uuid;
BEGIN
    SELECT type, category_id
    INTO attribute_type, attribute_category_id
    FROM category_attributes
    WHERE id = NEW.attribute_id;

    SELECT primary_category_id
    INTO product_category_id
    FROM products
    WHERE id = NEW.product_id;

    IF attribute_category_id IS NULL OR product_category_id IS NULL OR attribute_category_id <> product_category_id THEN
        RAISE EXCEPTION 'Attribute % does not belong to product % primary category.', NEW.attribute_id, NEW.product_id
            USING ERRCODE = 'check_violation';
    END IF;

    IF attribute_type = 'text' AND NEW.value_text IS NULL THEN
        RAISE EXCEPTION 'Text attribute % requires value_text.', NEW.attribute_id
            USING ERRCODE = 'check_violation';
    ELSIF attribute_type = 'number' AND NEW.value_number IS NULL THEN
        RAISE EXCEPTION 'Number attribute % requires value_number.', NEW.attribute_id
            USING ERRCODE = 'check_violation';
    ELSIF attribute_type = 'boolean' AND NEW.value_boolean IS NULL THEN
        RAISE EXCEPTION 'Boolean attribute % requires value_boolean.', NEW.attribute_id
            USING ERRCODE = 'check_violation';
    ELSIF attribute_type = 'select' THEN
        IF NEW.attribute_option_id IS NULL THEN
            RAISE EXCEPTION 'Select attribute % requires attribute_option_id.', NEW.attribute_id
                USING ERRCODE = 'check_violation';
        END IF;

        SELECT attribute_id
        INTO option_attribute_id
        FROM attribute_options
        WHERE id = NEW.attribute_option_id;

        IF option_attribute_id IS NULL OR option_attribute_id <> NEW.attribute_id THEN
            RAISE EXCEPTION 'Attribute option % does not belong to attribute %.', NEW.attribute_option_id, NEW.attribute_id
                USING ERRCODE = 'check_violation';
        END IF;
    END IF;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_product_attribute_values_validate
BEFORE INSERT OR UPDATE ON product_attribute_values
FOR EACH ROW
EXECUTE FUNCTION validate_product_attribute_value();
