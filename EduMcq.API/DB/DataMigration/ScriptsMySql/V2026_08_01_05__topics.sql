-- ============================================================
-- 4. TOPICS (Hierarchical topics under subjects)
-- ============================================================
CREATE TABLE IF NOT EXISTS topics (
    id              CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    subject_id      CHAR(36) NOT NULL REFERENCES subjects(id) ON DELETE CASCADE,
    parent_id       CHAR(36) REFERENCES topics(id) ON DELETE SET NULL,
    name            VARCHAR(200) NOT NULL,
    slug            VARCHAR(200) NOT NULL,
    depth_level     SMALLINT DEFAULT 1 CHECK (depth_level BETWEEN 1 AND 5),
    display_order   SMALLINT DEFAULT 0,
    created_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6),
    is_active       BOOLEAN DEFAULT true,
    updated_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6)
);
CREATE INDEX idx_topics_subject ON topics(subject_id);
CREATE INDEX idx_topics_parent ON topics(parent_id);