-- ============================================================
-- ARTICLES
-- ============================================================
CREATE TABLE IF NOT EXISTS articles (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    title           VARCHAR(300) NOT NULL,
    slug            VARCHAR(320) UNIQUE,                    -- optional, useful for SEO later
    excerpt         VARCHAR(500),
    content         TEXT NOT NULL,
    
    author_name     VARCHAR(200) NOT NULL DEFAULT 'Admin',
    author_avatar   VARCHAR(500),
    
    category        VARCHAR(100) NOT NULL,
    tags            TEXT[],                                 -- PostgreSQL array
    
    cover_image_url VARCHAR(500),
    read_time_minutes SMALLINT DEFAULT 5,
    
    is_featured     BOOLEAN DEFAULT FALSE,
    status          VARCHAR(20) NOT NULL DEFAULT 'draft'
                    CHECK (status IN ('draft', 'published', 'archived')),
    
    view_count      INTEGER DEFAULT 0,
    like_count      INTEGER DEFAULT 0,
    
    published_at    TIMESTAMPTZ,
    created_by      UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at      TIMESTAMPTZ DEFAULT now(),
    updated_at      TIMESTAMPTZ DEFAULT now()
);

-- Indexes
CREATE INDEX idx_articles_status     ON articles(status);
CREATE INDEX idx_articles_category   ON articles(category);
CREATE INDEX idx_articles_featured   ON articles(is_featured) WHERE is_featured = TRUE;
CREATE INDEX idx_articles_published  ON articles(published_at DESC) WHERE status = 'published';
CREATE INDEX idx_articles_tags       ON articles USING GIN(tags);   -- for tag search