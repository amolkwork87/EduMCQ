-- ============================================================
-- 11. QUESTION_IMAGES (Additional images per question)
-- ============================================================
CREATE TABLE IF NOT EXISTS question_images (
    id              CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    question_id     CHAR(36) NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    image_url       VARCHAR(500) NOT NULL,
    caption         VARCHAR(500),
    display_order   SMALLINT DEFAULT 0,
    image_type      VARCHAR(20) DEFAULT 'reference' CHECK (image_type IN ('question', 'hint', 'reference', 'solution')),
    created_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6)
);
CREATE INDEX idx_qimages_question ON question_images(question_id);