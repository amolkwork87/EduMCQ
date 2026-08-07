CREATE TABLE if not exists thinking_skills (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name            VARCHAR(100) NOT NULL UNIQUE,
    slug            VARCHAR(100) NOT NULL UNIQUE,
    description     TEXT,
    category        skill_category DEFAULT 'cognitive',
    created_at      TIMESTAMPTZ DEFAULT now()
);