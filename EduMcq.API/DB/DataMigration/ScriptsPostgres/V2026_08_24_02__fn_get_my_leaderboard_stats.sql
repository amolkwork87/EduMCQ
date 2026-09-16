CREATE OR REPLACE FUNCTION fn_get_my_leaderboard_stats(
    p_current_user_id UUID
)
RETURNS TABLE (
    global_rank    INT,
    total_points   INT,
    accuracy       NUMERIC,
    quizzes_taken  INT
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
        GROUP BY ta.user_id
    ),
    ranked AS (
        SELECT
            us.*,
            RANK() OVER (ORDER BY us.score DESC, us.accuracy DESC, us.quizzes DESC) AS rank
        FROM user_stats us
    )
    SELECT
        r.rank::INT          AS global_rank,
        r.score::INT         AS total_points,
        r.accuracy,
        r.quizzes::INT       AS quizzes_taken
    FROM ranked r
    WHERE r.user_id = p_current_user_id;
$$;