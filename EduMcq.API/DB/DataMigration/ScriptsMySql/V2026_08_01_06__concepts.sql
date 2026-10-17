-- ============================================================
-- 6. CONCEPTS (Specific concepts within topics)
-- ============================================================
CREATE TABLE IF NOT EXISTS concepts (
    id              CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    topic_id        CHAR(36) NOT NULL REFERENCES topics(id) ON DELETE CASCADE,
    name            VARCHAR(200) NOT NULL,
    description     TEXT,
    display_order   SMALLINT DEFAULT 0,
    is_active       BOOLEAN DEFAULT true,
    created_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6),
    updated_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6)
);
CREATE INDEX idx_concepts_topic ON concepts(topic_id);