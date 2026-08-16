-- ============================================================
-- 7. QUESTIONS (Main content table)
-- ============================================================
CREATE TABLE IF NOT EXISTS questions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    exam_id         UUID REFERENCES exams(id) ON DELETE SET NULL,
    standard_id     UUID REFERENCES standards(id) ON DELETE SET NULL,
    subject_id      UUID REFERENCES subjects(id) ON DELETE SET NULL,
     topic_id        UUID REFERENCES topics(id) ON DELETE SET NULL,  -- NEW
    concept_id      UUID REFERENCES concepts(id) ON DELETE SET NULL, -- NEW
    text            TEXT,
    image_url       VARCHAR(500),
    has_image_options BOOLEAN DEFAULT false,
    difficulty      VARCHAR(10) CHECK (difficulty IN ('easy', 'medium', 'hard')),
    status              VARCHAR(20) DEFAULT 'draft' CHECK (status IN ('draft', 'published', 'archived')),
    explanation     TEXT,
    exam_tags       VARCHAR(500),
    geogebra_xml    TEXT,
    content_hash    VARCHAR(64),
    created_by      UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at      TIMESTAMPTZ DEFAULT now(),
    updated_at      TIMESTAMPTZ DEFAULT now()
);

CREATE INDEX idx_questions_exam ON questions(exam_id);
CREATE INDEX idx_questions_standard ON questions(standard_id);
CREATE INDEX idx_questions_subject ON questions(subject_id);
CREATE INDEX idx_questions_status ON questions(status);
CREATE INDEX idx_questions_difficulty ON questions(difficulty);
CREATE INDEX idx_questions_content_hash ON questions(content_hash);
CREATE INDEX idx_questions_created_at ON questions(created_at DESC);
CREATE INDEX idx_questions_topic ON questions(topic_id);        -- NEW
CREATE INDEX idx_questions_concept ON questions(concept_id);    -- NEW