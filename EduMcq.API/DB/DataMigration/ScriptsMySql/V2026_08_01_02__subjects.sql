-- ============================================================
-- 3. SUBJECTS (Subjects within an exam)
-- ============================================================
CREATE TABLE IF NOT EXISTS subjects  (
    id              CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    exam_id         CHAR(36) NOT NULL REFERENCES exams(id) ON DELETE CASCADE,
    name            VARCHAR(100) NOT NULL, -- "Physics", "Chemistry", "Mathematics"
    slug            VARCHAR(100) NOT NULL,
    display_order   SMALLINT DEFAULT 0,
    UNIQUE (exam_id, name),
    created_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6),
    is_active       BOOLEAN DEFAULT true,
    updated_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6)
);

CREATE INDEX idx_subjects_exam ON subjects(exam_id);