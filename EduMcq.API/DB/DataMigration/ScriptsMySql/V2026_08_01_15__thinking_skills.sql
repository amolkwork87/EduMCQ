-- ============================================================
-- 5. THINKING_SKILLS (Bloom's taxonomy levels)
-- ============================================================
CREATE TABLE IF NOT EXISTS thinking_skills (
    id              CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    name            VARCHAR(100) NOT NULL UNIQUE, -- "Remember", "Understand", "Apply"
    slug            VARCHAR(100) NOT NULL UNIQUE,
    description     TEXT,
    category         VARCHAR(30) DEFAULT 'cognitive' CHECK (category IN ('cognitive', 'affective', 'psychomotor')),
    created_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6),
    is_active       BOOLEAN DEFAULT true,
    updated_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6)
);