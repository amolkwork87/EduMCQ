-- Table for test_answers
CREATE TABLE test_answers (
    id CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    attempt_id CHAR(36) NOT NULL,
    question_id CHAR(36) NOT NULL,
    selected_option TEXT,  -- or VARCHAR, depending on your option format
    is_correct BOOLEAN DEFAULT FALSE,
    created_at DATETIME(6) NULL DEFAULT CURRENT_TIMESTAMP(6),
    updated_at DATETIME(6) NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    CONSTRAINT fk_test_answers_attempt FOREIGN KEY (attempt_id) REFERENCES test_attempts(id) ON DELETE CASCADE,
    CONSTRAINT fk_test_answers_question FOREIGN KEY (question_id) REFERENCES questions(id) ON DELETE CASCADE,
    CONSTRAINT unique_attempt_question UNIQUE (attempt_id, question_id)
);



-- Optional: Create indexes for better performance
CREATE INDEX idx_test_answers_attempt_id ON test_answers(attempt_id);
CREATE INDEX idx_test_answers_question_id ON test_answers(question_id);


-- -- Optional: Create a trigger to automatically update updated_at timestamp
-- CREATE OR REPLACE FUNCTION update_updated_at_column()
-- RETURNS TRIGGER AS $$
-- BEGIN
--     NEW.updated_at = CURRENT_TIMESTAMP;
--     RETURN NEW;
-- END;
-- $$ language 'plpgsql';

-- CREATE TRIGGER update_test_answers_updated_at 
--     BEFORE UPDATE ON test_answers 
--     FOR EACH ROW 
--     EXECUTE FUNCTION update_updated_at_column();

-- CREATE TRIGGER update_test_attempts_updated_at 
--     BEFORE UPDATE ON test_attempts 
--     FOR EACH ROW 
--     EXECUTE FUNCTION update_updated_at_column();