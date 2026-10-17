CREATE TABLE test_attempts (
    id CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    user_id CHAR(36) NOT NULL,
    subject_id CHAR(36) NOT NULL,
    standard_id CHAR(36) NOT NULL,
    total_questions INTEGER NOT NULL,
    correct_count INTEGER DEFAULT 0,
    wrong_count INTEGER DEFAULT 0,
    unanswered_count INTEGER DEFAULT 0,
    score_percentage DECIMAL(5,2) DEFAULT 0.00,
    time_taken_seconds INTEGER DEFAULT 0,
    started_at DATETIME(6) NOT NULL,
    completed_at DATETIME(6) NOT NULL,
    created_at DATETIME(6) NULL DEFAULT CURRENT_TIMESTAMP(6),
    updated_at DATETIME(6) NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    /* CONSTRAINT fk_test_attempts_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE, */
    CONSTRAINT fk_test_attempts_subject FOREIGN KEY (subject_id) REFERENCES subjects(id) ON DELETE CASCADE,
    CONSTRAINT fk_test_attempts_standard FOREIGN KEY (standard_id) REFERENCES standards(id) ON DELETE CASCADE
);

CREATE INDEX idx_test_attempts_user_id ON test_attempts(user_id);
CREATE INDEX idx_test_attempts_subject_id ON test_attempts(subject_id);
CREATE INDEX idx_test_attempts_standard_id ON test_attempts(standard_id);