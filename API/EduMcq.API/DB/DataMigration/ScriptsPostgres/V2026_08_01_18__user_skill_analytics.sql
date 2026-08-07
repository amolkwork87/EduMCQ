CREATE TABLE if not exists  user_skill_analytics (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    skill_id        UUID NOT NULL REFERENCES thinking_skills(id) ON DELETE CASCADE,
    total_attempted INTEGER DEFAULT 0,
    correct_count   INTEGER DEFAULT 0,
    accuracy_pct    DECIMAL(5,2) DEFAULT 0,
    weakness_level  weakness_level DEFAULT 'average',
    last_assessed   TIMESTAMPTZ,
    created_at      TIMESTAMPTZ DEFAULT now(),
    updated_at      TIMESTAMPTZ DEFAULT now(),
    UNIQUE (user_id, skill_id)
);
CREATE INDEX idx_usa_user_level ON user_skill_analytics(user_id, weakness_level);