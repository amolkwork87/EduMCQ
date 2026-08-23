-- ============================================================
-- v_user_monthly_usage  (plain view – always fresh)
-- ============================================================
CREATE OR REPLACE VIEW v_user_monthly_usage AS
SELECT
    user_id,
    date_trunc('month', completed_at)::date          AS month_start,
    COUNT(*)                                         AS quizzes_taken,
    COALESCE(SUM(total_questions), 0)                AS questions_attempted
FROM test_attempts
WHERE completed_at IS NOT NULL
GROUP BY user_id, date_trunc('month', completed_at)::date;