-- ============================================================
-- 4. TOPICS (Hierarchical topics under subjects)
-- ============================================================
CREATE TABLE IF NOT EXISTS topics (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subject_id      UUID NOT NULL REFERENCES subjects(id) ON DELETE CASCADE,
    parent_id       UUID REFERENCES topics(id) ON DELETE SET NULL,
    name            VARCHAR(200) NOT NULL,
    slug            VARCHAR(200) NOT NULL,
    depth_level     SMALLINT DEFAULT 1 CHECK (depth_level BETWEEN 1 AND 5),
    display_order   SMALLINT DEFAULT 0,
    created_at      TIMESTAMPTZ DEFAULT now(),
    is_active       BOOLEAN DEFAULT true,
    updated_at      TIMESTAMPTZ DEFAULT now()
);
CREATE INDEX idx_topics_subject ON topics(subject_id);
CREATE INDEX idx_topics_parent ON topics(parent_id);