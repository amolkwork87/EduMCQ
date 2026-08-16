-- ============================================================
-- 5. THINKING_SKILLS (Bloom's taxonomy levels)
-- ============================================================
CREATE TABLE IF NOT EXISTS thinking_skills (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(100) NOT NULL UNIQUE, -- "Remember", "Understand", "Apply"
    slug            VARCHAR(100) NOT NULL UNIQUE,
    description     TEXT,
    category         TEXT DEFAULT 'cognitive' CHECK (category IN ('cognitive', 'affective', 'psychomotor')),
    created_at      TIMESTAMPTZ DEFAULT now(),
    is_active       BOOLEAN DEFAULT true,
    updated_at      TIMESTAMPTZ DEFAULT now()
);