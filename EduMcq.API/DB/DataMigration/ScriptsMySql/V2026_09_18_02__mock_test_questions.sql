CREATE TABLE IF NOT EXISTS mock_test_questions (
    id CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    template_id CHAR(36) NOT NULL REFERENCES mock_test_templates(id) ON DELETE CASCADE,
    question_id CHAR(36) NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    display_order INT NOT NULL DEFAULT 0,
    marks NUMERIC(5,2) DEFAULT 1,
    negative_marks NUMERIC(5,2) DEFAULT 0,
    created_at DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6),
    UNIQUE(template_id, question_id)   -- same question can't be added twice to SAME test
);

-- Same question CAN appear in multiple templates (composite unique allows this)
CREATE INDEX idx_mtq_template ON mock_test_questions(template_id);
CREATE INDEX idx_mtq_question ON mock_test_questions(question_id);
CREATE INDEX idx_questions_filter ON questions(exam_id, standard_id, subject_id, status);