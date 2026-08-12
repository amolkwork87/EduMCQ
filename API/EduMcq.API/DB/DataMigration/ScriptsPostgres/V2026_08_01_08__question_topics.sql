-- ============================================================
-- 9. QUESTION_TOPICS (M:N junction)
-- ============================================================
CREATE TABLE IF NOT EXISTS question_topics (
    question_id     UUID NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    topic_id        UUID NOT NULL REFERENCES topics(id) ON DELETE CASCADE,
    PRIMARY KEY (question_id, topic_id)
);
CREATE INDEX idx_qt_topic ON question_topics(topic_id);