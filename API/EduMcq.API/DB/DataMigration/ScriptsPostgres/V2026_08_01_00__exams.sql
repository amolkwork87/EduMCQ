CREATE TABLE if not exists exams (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(100) NOT NULL UNIQUE,
    slug            VARCHAR(100) NOT NULL UNIQUE,
    description     TEXT,
    logo_url        VARCHAR(500),
    is_active       BOOLEAN DEFAULT true,
    display_order   SMALLINT DEFAULT 0,
    created_at      TIMESTAMPTZ DEFAULT now()
);