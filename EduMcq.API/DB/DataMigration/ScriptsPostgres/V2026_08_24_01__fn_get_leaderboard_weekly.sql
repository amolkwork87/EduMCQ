CREATE OR REPLACE FUNCTION fn_get_leaderboard_weekly(
    p_current_user_id UUID,
    p_exam           TEXT DEFAULT 'all',
    p_limit          INT  DEFAULT 50
)
RETURNS TABLE (
    rank            INT,
    name            TEXT,
    score           INT,
    quizzes         INT,
    accuracy        NUMERIC,
    is_current_user BOOLEAN
)
LANGUAGE sql
STABLE
AS $$
    WITH user_stats AS (
        SELECT
            ta.user_id,
            COUNT(*)                            AS quizzes,
            COALESCE(SUM(ta.correct_count), 0)  AS score,
            ROUND(AVG(ta.score_percentage)::numeric, 1) AS accuracy
        FROM test_attempts ta
        WHERE ta.completed_at >= date_trunc('week', CURRENT_TIMESTAMP)
        -- Optional exam filter (uncomment when ready)
        -- JOIN subjects s ON s.id = ta.subject_id
        -- JOIN exams e    ON e.id = s.exam_id
        -- WHERE (p_exam = 'all' OR LOWER(e.name) = LOWER(p_exam))
        GROUP BY ta.user_id
    ),
    ranked AS (
        SELECT
            us.*,
            RANK() OVER (ORDER BY us.score DESC, us.accuracy DESC, us.quizzes DESC) AS rank
        FROM user_stats us
    )
    SELECT
        r.rank::INT,
        COALESCE(u.name, u.email, 'Anonymous') AS name,
        r.score::INT,
        r.quizzes::INT,
        r.accuracy,
        (r.user_id = p_current_user_id) AS is_current_user
    FROM ranked r
    JOIN users u ON u.id = r.user_id
    ORDER BY r.rank
    LIMIT p_limit;
$$;