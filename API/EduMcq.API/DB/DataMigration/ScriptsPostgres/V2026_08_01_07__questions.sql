CREATE TABLE if not exists  questions (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    exam_id         UUID REFERENCES exams(id) ON DELETE SET NULL,
    standard_id     UUID REFERENCES standards(id) ON DELETE SET NULL,
    subject_id      UUID REFERENCES subjects(id) ON DELETE SET NULL,
    text            TEXT,
    image_url       VARCHAR(500),
    has_image_options BOOLEAN DEFAULT false,
    difficulty      difficulty_level DEFAULT 'medium',
    status          question_status DEFAULT 'draft',
    explanation     TEXT,
    exam_tags       VARCHAR(500),
    geogebra_xml    TEXT,
    content_hash    VARCHAR(64),
    created_by      UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at      TIMESTAMPTZ DEFAULT now(),
    updated_at      TIMESTAMPTZ DEFAULT now()
);