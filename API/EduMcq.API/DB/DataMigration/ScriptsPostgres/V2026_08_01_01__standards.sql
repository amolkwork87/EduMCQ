-- ============================================================
-- 2. STANDARDS (Classes/Grades within an exam)
-- ============================================================
CREATE TABLE IF NOT EXISTS standards(
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    exam_id         UUID NOT NULL REFERENCES exams(id) ON DELETE CASCADE,
    name            VARCHAR(50) NOT NULL, -- "Class 10", "Class 11", "Class 12"
    slug            VARCHAR(50) NOT NULL,
    display_order   SMALLINT DEFAULT 0,
    UNIQUE (exam_id, name),
    created_at      TIMESTAMPTZ DEFAULT now(),
    updated_at      TIMESTAMPTZ DEFAULT now(),
    is_active       BOOLEAN DEFAULT true
);

CREATE INDEX idx_standards_exam ON standards(exam_id);