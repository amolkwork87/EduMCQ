-- ============================================================
-- 8. OPTIONS (4 options per question)
-- ============================================================
CREATE TABLE IF NOT EXISTS options (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    question_id     UUID NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    label           CHAR(1) NOT NULL CHECK (label IN ('A','B','C','D')),
    text            TEXT,
    image_url       VARCHAR(500),
    is_correct      BOOLEAN DEFAULT false,
    position        SMALLINT DEFAULT 0,
    CHECK (
        (text IS NOT NULL AND LENGTH(TRIM(text)) > 0) OR
        (image_url IS NOT NULL AND LENGTH(TRIM(image_url)) > 0)
    )
);
CREATE INDEX idx_options_question ON options(question_id);