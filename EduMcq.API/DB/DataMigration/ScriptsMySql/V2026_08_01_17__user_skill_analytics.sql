CREATE TABLE if not exists  user_skill_analytics (
    id              CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    user_id         CHAR(36) NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    skill_id        CHAR(36) NOT NULL REFERENCES thinking_skills(id) ON DELETE CASCADE,
    total_attempted INTEGER DEFAULT 0,
    correct_count   INTEGER DEFAULT 0,
    accuracy_pct    DECIMAL(5,2) DEFAULT 0,
    weakness_level  VARCHAR(20) DEFAULT 'average',
    last_assessed   DATETIME(6),
    created_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6),
    updated_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6),
    UNIQUE (user_id, skill_id)
);
CREATE INDEX idx_usa_user_level ON user_skill_analytics(user_id, weakness_level);