CREATE TABLE if not exists  question_images (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    question_id     UUID NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    image_url       VARCHAR(500) NOT NULL,
    caption         VARCHAR(500),
    display_order   SMALLINT DEFAULT 0,
    image_type      TEXT DEFAULT 'question',
    created_at      TIMESTAMPTZ DEFAULT now()
);
CREATE INDEX idx_qimages_question ON question_images(question_id);