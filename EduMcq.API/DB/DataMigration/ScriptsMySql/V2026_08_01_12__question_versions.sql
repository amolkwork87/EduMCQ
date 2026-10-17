CREATE TABLE IF NOT EXISTS question_versions (
    id              CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    question_id     CHAR(36) NOT NULL,
    version_number  INTEGER NOT NULL,
    text            TEXT,
    image_url       VARCHAR(500),
    options_snapshot JSON,                          -- ← changed from JSONB
    changed_by      CHAR(36),
    change_reason   VARCHAR(500),
    created_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6),

    UNIQUE (question_id, version_number),

    CONSTRAINT fk_qversions_question 
        FOREIGN KEY (question_id) REFERENCES questions(id) ON DELETE CASCADE,
    CONSTRAINT fk_qversions_changed_by 
        FOREIGN KEY (changed_by) REFERENCES users(id) ON DELETE SET NULL
);

CREATE INDEX idx_qversions_question ON question_versions(question_id);