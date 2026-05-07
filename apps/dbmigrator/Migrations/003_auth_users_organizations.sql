CREATE TABLE users (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name text NOT NULL,
    email citext NULL,
    phone citext NULL,
    password_hash text NOT NULL,
    role text NOT NULL DEFAULT 'customer',
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_users_name_not_blank CHECK (btrim(name) <> ''),
    CONSTRAINT ck_users_email_not_blank CHECK (email IS NULL OR btrim(email::text) <> ''),
    CONSTRAINT ck_users_phone_not_blank CHECK (phone IS NULL OR btrim(phone::text) <> ''),
    CONSTRAINT ck_users_contact_required CHECK (email IS NOT NULL OR phone IS NOT NULL),
    CONSTRAINT ck_users_password_hash_not_blank CHECK (btrim(password_hash) <> ''),
    CONSTRAINT ck_users_role CHECK (role IN ('customer', 'seller', 'admin'))
);

CREATE UNIQUE INDEX ux_users_email ON users (email) WHERE email IS NOT NULL;
CREATE UNIQUE INDEX ux_users_phone ON users (phone) WHERE phone IS NOT NULL;
CREATE INDEX ix_users_role_active ON users (role, is_active);

CREATE TABLE organizations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    name text NOT NULL,
    inn text NULL,
    contact_person text NULL,
    phone text NULL,
    email citext NULL,
    comment text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_organizations_name_not_blank CHECK (btrim(name) <> ''),
    CONSTRAINT ck_organizations_inn_not_blank CHECK (inn IS NULL OR btrim(inn) <> ''),
    CONSTRAINT ck_organizations_contact_person_not_blank CHECK (contact_person IS NULL OR btrim(contact_person) <> ''),
    CONSTRAINT ck_organizations_phone_not_blank CHECK (phone IS NULL OR btrim(phone) <> ''),
    CONSTRAINT ck_organizations_email_not_blank CHECK (email IS NULL OR btrim(email::text) <> ''),
    CONSTRAINT ck_organizations_comment_not_blank CHECK (comment IS NULL OR btrim(comment) <> '')
);

CREATE UNIQUE INDEX ux_organizations_user_id ON organizations (user_id);
CREATE INDEX ix_organizations_inn ON organizations (inn) WHERE inn IS NOT NULL;

CREATE TRIGGER trg_users_set_updated_at
BEFORE UPDATE ON users
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();

CREATE TRIGGER trg_organizations_set_updated_at
BEFORE UPDATE ON organizations
FOR EACH ROW
EXECUTE FUNCTION set_updated_at();
