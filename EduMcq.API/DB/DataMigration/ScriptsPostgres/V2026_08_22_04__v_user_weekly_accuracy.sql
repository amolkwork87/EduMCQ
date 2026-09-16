-- ============================================================
-- mv_user_weekly_accuracy
-- ============================================================


CREATE OR REPLACE VIEW mv_user_weekly_accuracy AS
SELECT
    ta.user_id,
    date_trunc('week', ta.completed_at)::date                       AS week_start,
    ROUND(AVG(ta.score_percentage)::numeric, 2)                     AS avg_accuracy,
    COUNT(*)                                                        AS quizzes_in_week
FROM test_attempts ta
GROUP BY ta.user_id, date_trunc('week', ta.completed_at)::date;

-- CREATE UNIQUE INDEX idx_mv_user_weekly_pk
--     ON mv_user_weekly_accuracy (user_id, week_start);

-- CREATE INDEX idx_mv_user_weekly_user
--     ON mv_user_weekly_accuracy (user_id);