CREATE TABLE request_number_counters (
    year integer PRIMARY KEY,
    next_sequence integer NOT NULL DEFAULT 1,
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_request_number_counters_year CHECK (year >= 2000 AND year <= 9999),
    CONSTRAINT ck_request_number_counters_next_sequence_positive CHECK (next_sequence > 0)
);

CREATE TABLE requests (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    number text NOT NULL,
    number_year integer NOT NULL,
    number_sequence integer NOT NULL,
    user_id uuid NOT NULL REFERENCES users (id) ON DELETE RESTRICT,
    organization_id uuid NULL REFERENCES organizations (id) ON DELETE SET NULL,
    status text NOT NULL DEFAULT 'new',
    source text NOT NULL,
    customer_name text NOT NULL,
    customer_email citext NULL,
    customer_phone citext NULL,
    organization_name text NULL,
    organization_inn text NULL,
    organization_contact_person text NULL,
    organization_phone text NULL,
    organization_email citext NULL,
    customer_comment text NULL,
    internal_comment text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_requests_number_not_blank CHECK (btrim(number) <> ''),
    CONSTRAINT ck_requests_number_year CHECK (number_year >= 2000 AND number_year <= 9999),
    CONSTRAINT ck_requests_number_sequence_positive CHECK (number_sequence > 0),
    CONSTRAINT ck_requests_status CHECK (status IN ('new', 'in_progress', 'completed', 'cancelled')),
    CONSTRAINT ck_requests_source CHECK (source IN ('cart', 'quick_order')),
    CONSTRAINT ck_requests_customer_name_not_blank CHECK (btrim(customer_name) <> ''),
    CONSTRAINT ck_requests_customer_email_not_blank CHECK (customer_email IS NULL OR btrim(customer_email::text) <> ''),
    CONSTRAINT ck_requests_customer_phone_not_blank CHECK (customer_phone IS NULL OR btrim(customer_phone::text) <> ''),
    CONSTRAINT ck_requests_customer_contact_required CHECK (customer_email IS NOT NULL OR customer_phone IS NOT NULL),
    CONSTRAINT ck_requests_organization_name_not_blank CHECK (organization_name IS NULL OR btrim(organization_name) <> ''),
    CONSTRAINT ck_requests_organization_inn_not_blank CHECK (organization_inn IS NULL OR btrim(organization_inn) <> ''),
    CONSTRAINT ck_requests_organization_contact_person_not_blank CHECK (organization_contact_person IS NULL OR btrim(organization_contact_person) <> ''),
    CONSTRAINT ck_requests_organization_phone_not_blank CHECK (organization_phone IS NULL OR btrim(organization_phone) <> ''),
    CONSTRAINT ck_requests_organization_email_not_blank CHECK (organization_email IS NULL OR btrim(organization_email::text) <> ''),
    CONSTRAINT ck_requests_customer_comment_not_blank CHECK (customer_comment IS NULL OR btrim(customer_comment) <> ''),
    CONSTRAINT ck_requests_internal_comment_not_blank CHECK (internal_comment IS NULL OR btrim(internal_comment) <> '')
);

CREATE UNIQUE INDEX ux_requests_number ON requests (number);
CREATE UNIQUE INDEX ux_requests_number_year_sequence ON requests (number_year, number_sequence);
CREATE INDEX ix_requests_user_id_created_at ON requests (user_id, created_at DESC);
CREATE INDEX ix_requests_status_created_at ON requests (status, created_at DESC);

CREATE TABLE request_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id uuid NOT NULL REFERENCES requests (id) ON DELETE CASCADE,
    product_id uuid NOT NULL REFERENCES products (id) ON DELETE RESTRICT,
    quantity numeric(18, 3) NOT NULL,
    product_name text NOT NULL,
    product_slug text NOT NULL,
    product_sku text NULL,
    category_name text NOT NULL,
    category_slug text NOT NULL,
    brand_name text NULL,
    brand_slug text NULL,
    availability_status text NOT NULL,
    sale_unit text NOT NULL,
    unit_quantity text NOT NULL,
    customer_comment text NULL,
    sort_order integer NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_request_items_quantity_positive CHECK (quantity > 0),
    CONSTRAINT ck_request_items_product_name_not_blank CHECK (btrim(product_name) <> ''),
    CONSTRAINT ck_request_items_product_slug_not_blank CHECK (btrim(product_slug) <> ''),
    CONSTRAINT ck_request_items_product_sku_not_blank CHECK (product_sku IS NULL OR btrim(product_sku) <> ''),
    CONSTRAINT ck_request_items_category_name_not_blank CHECK (btrim(category_name) <> ''),
    CONSTRAINT ck_request_items_category_slug_not_blank CHECK (btrim(category_slug) <> ''),
    CONSTRAINT ck_request_items_brand_name_not_blank CHECK (brand_name IS NULL OR btrim(brand_name) <> ''),
    CONSTRAINT ck_request_items_brand_slug_not_blank CHECK (brand_slug IS NULL OR btrim(brand_slug) <> ''),
    CONSTRAINT ck_request_items_availability_status CHECK (availability_status IN ('in_stock', 'on_order', 'check_availability')),
    CONSTRAINT ck_request_items_sale_unit CHECK (sale_unit IN ('coil', 'box', 'piece', 'pack')),
    CONSTRAINT ck_request_items_unit_quantity_not_blank CHECK (btrim(unit_quantity) <> ''),
    CONSTRAINT ck_request_items_customer_comment_not_blank CHECK (customer_comment IS NULL OR btrim(customer_comment) <> '')
);

CREATE INDEX ix_request_items_request_id_sort_order ON request_items (request_id, sort_order);
CREATE INDEX ix_request_items_product_id ON request_items (product_id);

CREATE TABLE request_history (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id uuid NOT NULL REFERENCES requests (id) ON DELETE CASCADE,
    event_type text NOT NULL,
    actor_user_id uuid NULL REFERENCES users (id) ON DELETE SET NULL,
    old_status text NULL,
    new_status text NULL,
    comment text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_request_history_event_type CHECK (event_type IN ('created', 'status_changed', 'comment_added', 'items_changed')),
    CONSTRAINT ck_request_history_old_status CHECK (old_status IS NULL OR old_status IN ('new', 'in_progress', 'completed', 'cancelled')),
    CONSTRAINT ck_request_history_new_status CHECK (new_status IS NULL OR new_status IN ('new', 'in_progress', 'completed', 'cancelled')),
    CONSTRAINT ck_request_history_comment_not_blank CHECK (comment IS NULL OR btrim(comment) <> '')
);

CREATE INDEX ix_request_history_request_id_created_at ON request_history (request_id, created_at);
CREATE INDEX ix_request_history_actor_user_id ON request_history (actor_user_id) WHERE actor_user_id IS NOT NULL;

CREATE TRIGGER trg_requests_set_updated_at
BEFORE UPDATE ON requests
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_request_number_counters_set_updated_at
BEFORE UPDATE ON request_number_counters
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();
