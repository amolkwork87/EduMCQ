-- ============================================================
-- ARTICLES
-- ============================================================
CREATE TABLE IF NOT EXISTS articles (
    id              CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    
    title           VARCHAR(300) NOT NULL,
    slug            VARCHAR(320) UNIQUE,                    -- optional, useful for SEO later
    excerpt         VARCHAR(500),
    content         TEXT NOT NULL,
    
    author_name     VARCHAR(200) NOT NULL DEFAULT 'Admin',
    author_avatar   VARCHAR(500),
    
    category        VARCHAR(100) NOT NULL,
    tags            JSON,
    
    cover_image_url VARCHAR(500),
    read_time_minutes SMALLINT DEFAULT 5,
    
    is_featured     BOOLEAN DEFAULT FALSE,
    status          VARCHAR(20) NOT NULL DEFAULT 'draft'
                    CHECK (status IN ('draft', 'published', 'archived')),
    
    view_count      INTEGER DEFAULT 0,
    like_count      INTEGER DEFAULT 0,
    
    published_at    DATETIME(6),
    created_by      CHAR(36) REFERENCES users(id) ON DELETE SET NULL,
    created_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6),
    updated_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6)
);

-- Indexes
CREATE INDEX idx_articles_status     ON articles(status);
CREATE INDEX idx_articles_category   ON articles(category);
CREATE INDEX idx_articles_featured   ON articles(is_featured);
CREATE INDEX idx_articles_published  ON articles(published_at DESC);
