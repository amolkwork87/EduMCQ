CREATE TABLE if not exists standards (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    exam_id         UUID NOT NULL REFERENCES exams(id) ON DELETE CASCADE,
    name            VARCHAR(50) NOT NULL,
    slug            VARCHAR(50) NOT NULL,
    display_order   SMALLINT DEFAULT 0,
    UNIQUE (exam_id, name),
    created_at      TIMESTAMPTZ DEFAULT now()
);