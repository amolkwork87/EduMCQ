-- ============================================================
-- 6. CONCEPTS (Specific concepts within topics)
-- ============================================================
CREATE TABLE IF NOT EXISTS concepts (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    topic_id        UUID NOT NULL REFERENCES topics(id) ON DELETE CASCADE,
    name            VARCHAR(200) NOT NULL,
    description     TEXT,
    display_order   SMALLINT DEFAULT 0,
    is_active       BOOLEAN DEFAULT true,
    created_at      TIMESTAMPTZ DEFAULT now(),
    updated_at      TIMESTAMPTZ DEFAULT now()
);
CREATE INDEX idx_concepts_topic ON concepts(topic_id);