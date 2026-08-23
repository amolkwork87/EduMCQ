-- ============================================================
-- 3a. TOPIC_STANDARDS (Many-to-many: topics ↔ standards)
-- Links topics to the standards they are taught in.
-- A topic can span multiple standards (e.g., "Fractions" in Class 5 & 6).
-- A standard has many topics.
-- ============================================================
CREATE TABLE IF NOT EXISTS topic_standards (
    topic_id        UUID NOT NULL REFERENCES topics(id) ON DELETE CASCADE,
    standard_id     UUID NOT NULL REFERENCES standards(id) ON DELETE CASCADE,
    created_at      TIMESTAMPTZ DEFAULT now(),
    PRIMARY KEY (topic_id, standard_id)
);

CREATE INDEX idx_topic_standards_topic ON topic_standards(topic_id);
CREATE INDEX idx_topic_standards_standard ON topic_standards(standard_id);