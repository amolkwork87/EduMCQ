CREATE TABLE if not exists thinking_skills (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(100) NOT NULL UNIQUE,
    slug            VARCHAR(100) NOT NULL UNIQUE,
    description     TEXT,
    category         TEXT DEFAULT 'cognitive',
    created_at      TIMESTAMPTZ DEFAULT now()
);