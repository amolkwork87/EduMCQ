CREATE TABLE if not exists  question_versions (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    question_id     UUID NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    version_number  INTEGER NOT NULL,
    text            TEXT,
    image_url       VARCHAR(500),
    options_snapshot JSONB,
    changed_by      UUID REFERENCES users(id) ON DELETE SET NULL,
    change_reason   VARCHAR(500),
    created_at      TIMESTAMPTZ DEFAULT now(),
    UNIQUE (question_id, version_number)
);
CREATE INDEX idx_qversions_question ON question_versions(question_id);