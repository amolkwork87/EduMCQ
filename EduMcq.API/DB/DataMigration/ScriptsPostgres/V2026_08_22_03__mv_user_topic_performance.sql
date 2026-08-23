-- ============================================================
-- mv_user_topic_performance
-- ============================================================
DROP MATERIALIZED VIEW IF EXISTS mv_user_topic_performance CASCADE;

CREATE MATERIALIZED VIEW mv_user_topic_performance AS
SELECT
    ta.user_id,
    q.topic_id,
    t.name                                                          AS topic_name,
    COUNT(*)                                                        AS questions_attempted,
    COUNT(*) FILTER (WHERE tans.is_correct = true)                  AS correct_count,
    ROUND(
        (100.0 * COUNT(*) FILTER (WHERE tans.is_correct = true) / NULLIF(COUNT(*), 0))::numeric,
        2
    )                                                               AS accuracy,
    MAX(ta.completed_at)                                            AS last_attempt_at
FROM test_attempts ta
JOIN test_answers tans ON tans.attempt_id = ta.id
JOIN questions q       ON q.id = tans.question_id
JOIN topics t          ON t.id = q.topic_id
WHERE q.topic_id IS NOT NULL
GROUP BY ta.user_id, q.topic_id, t.name;

CREATE UNIQUE INDEX idx_mv_user_topic_pk
    ON mv_user_topic_performance (user_id, topic_id);

CREATE INDEX idx_mv_user_topic_user
    ON mv_user_topic_performance (user_id);