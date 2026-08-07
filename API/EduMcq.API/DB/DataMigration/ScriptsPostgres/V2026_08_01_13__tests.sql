CREATE TABLE if not exists  tests (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    exam_id         UUID REFERENCES exams(id) ON DELETE SET NULL,
    standard_id     UUID REFERENCES standards(id) ON DELETE SET NULL,
    subject_id      UUID REFERENCES subjects(id) ON DELETE SET NULL,
    title           VARCHAR(300) NOT NULL,
    description     TEXT,
    time_limit_sec  INTEGER NOT NULL DEFAULT 1800,
    question_count  INTEGER NOT NULL DEFAULT 20,
    is_premium      BOOLEAN DEFAULT false,
    status          question_status DEFAULT 'draft',
    created_by      UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at      TIMESTAMPTZ DEFAULT now(),
    updated_at      TIMESTAMPTZ DEFAULT now()
);