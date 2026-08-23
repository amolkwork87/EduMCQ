-- ============================================================
-- 3. SUBJECTS (Subjects within an exam)
-- ============================================================
CREATE TABLE IF NOT EXISTS subjects  (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    exam_id         UUID NOT NULL REFERENCES exams(id) ON DELETE CASCADE,
    name            VARCHAR(100) NOT NULL, -- "Physics", "Chemistry", "Mathematics"
    slug            VARCHAR(100) NOT NULL,
    display_order   SMALLINT DEFAULT 0,
    UNIQUE (exam_id, name),
    created_at      TIMESTAMPTZ DEFAULT now(),
    is_active       BOOLEAN DEFAULT true,
    updated_at      TIMESTAMPTZ DEFAULT now()
);

CREATE INDEX idx_subjects_exam ON subjects(exam_id);