-- ============================================================
-- 1a. STANDARD_SUBJECTS (Junction: which subjects in which class)
-- ============================================================
CREATE TABLE IF NOT EXISTS standard_subjects (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    standard_id     UUID NOT NULL REFERENCES standards(id) ON DELETE CASCADE,
    subject_id      UUID NOT NULL REFERENCES subjects(id) ON DELETE CASCADE,
    is_active       BOOLEAN DEFAULT true,
    created_at      TIMESTAMPTZ DEFAULT now(),
    UNIQUE(standard_id, subject_id)
);

CREATE INDEX idx_std_subjects_standard ON standard_subjects(standard_id);
CREATE INDEX idx_std_subjects_subject ON standard_subjects(subject_id);