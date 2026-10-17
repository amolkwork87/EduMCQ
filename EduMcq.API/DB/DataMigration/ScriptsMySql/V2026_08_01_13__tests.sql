CREATE TABLE if not exists  tests (
    id              CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    exam_id         CHAR(36) REFERENCES exams(id) ON DELETE SET NULL,
    standard_id     CHAR(36) REFERENCES standards(id) ON DELETE SET NULL,
    subject_id      CHAR(36) REFERENCES subjects(id) ON DELETE SET NULL,
    title           VARCHAR(300) NOT NULL,
    description     TEXT,
    time_limit_sec  INTEGER NOT NULL DEFAULT 1800,
    question_count  INTEGER NOT NULL DEFAULT 20,
    is_premium      BOOLEAN DEFAULT false,
    status          VARCHAR(20) DEFAULT 'draft',
    created_by      CHAR(36) REFERENCES users(id) ON DELETE SET NULL,
    created_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6),
    updated_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6)
);