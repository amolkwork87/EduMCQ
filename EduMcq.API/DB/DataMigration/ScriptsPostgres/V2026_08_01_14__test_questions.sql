CREATE TABLE if not exists  test_questions (
    test_id         UUID NOT NULL REFERENCES tests(id) ON DELETE CASCADE,
    question_id     UUID NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    display_order   SMALLINT DEFAULT 0,
    marks           SMALLINT DEFAULT 1,
    PRIMARY KEY (test_id, question_id)
);