DROP MATERIALIZED VIEW IF EXISTS mv_user_progress CASCADE;

CREATE MATERIALIZED VIEW mv_user_progress AS
SELECT
    ta.user_id,

    COUNT(*)                                                        AS quizzes_taken,          -- pure test attempts
    ROUND(AVG(ta.score_percentage)::numeric, 2)                     AS avg_accuracy,
    COALESCE(SUM(ta.time_taken_seconds), 0)                         AS total_time_seconds,
    ROUND(AVG(ta.score_percentage)::numeric, 2)                     AS user_avg_score,
    MAX(ta.completed_at)                                            AS last_attempt_at,
    MIN(ta.completed_at)                                            AS first_attempt_at

FROM test_attempts ta
GROUP BY ta.user_id;

CREATE UNIQUE INDEX idx_mv_user_progress_user
    ON mv_user_progress (user_id);