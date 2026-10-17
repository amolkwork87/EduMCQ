-- ============================================================
-- 1. EXAMS (Top-level exam boards)
-- ============================================================
CREATE TABLE IF NOT EXISTS  exams (
    id              CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    name            VARCHAR(100) NOT NULL UNIQUE,
    slug            VARCHAR(100) NOT NULL UNIQUE,
    description     TEXT,
    logo_url        VARCHAR(500),
    is_active       BOOLEAN DEFAULT true,
    display_order   SMALLINT DEFAULT 0,
    created_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6),
    updated_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6)
);