-- Table for test_attempts
CREATE TABLE test_attempts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    subject_id UUID NOT NULL,
    standard_id UUID NOT NULL,
    total_questions INTEGER NOT NULL,
    correct_count INTEGER DEFAULT 0,
    wrong_count INTEGER DEFAULT 0,
    unanswered_count INTEGER DEFAULT 0,
    score_percentage DECIMAL(5,2) DEFAULT 0.00,  -- for percentage with 2 decimal places
    time_taken_seconds INTEGER DEFAULT 0,
    started_at TIMESTAMP WITH TIME ZONE NOT NULL,
    completed_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    --CONSTRAINT fk_test_attempts_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_test_attempts_subject FOREIGN KEY (subject_id) REFERENCES subjects(id) ON DELETE CASCADE,
    CONSTRAINT fk_test_attempts_standard FOREIGN KEY (standard_id) REFERENCES standards(id) ON DELETE CASCADE
);

CREATE INDEX idx_test_attempts_user_id ON test_attempts(user_id);
CREATE INDEX idx_test_attempts_subject_id ON test_attempts(subject_id);
CREATE INDEX idx_test_attempts_standard_id ON test_attempts(standard_id);