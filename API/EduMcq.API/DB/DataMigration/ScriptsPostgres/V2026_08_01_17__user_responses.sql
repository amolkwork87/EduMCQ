CREATE TABLE if not exists  user_responses (
    id              BIGSERIAL PRIMARY KEY,
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    test_id         UUID NOT NULL REFERENCES tests(id) ON DELETE CASCADE,
    question_id     UUID NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    selected_option_id UUID REFERENCES options(id) ON DELETE SET NULL,
    time_spent_sec  INTEGER DEFAULT 0,
    is_correct      BOOLEAN,
    created_at      TIMESTAMPTZ DEFAULT now()
);

CREATE INDEX idx_ur_user_test ON user_responses(user_id, test_id);
CREATE INDEX idx_ur_question ON user_responses(question_id);