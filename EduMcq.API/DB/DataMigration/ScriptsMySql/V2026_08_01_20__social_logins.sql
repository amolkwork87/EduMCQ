CREATE TABLE IF NOT EXISTS social_logins (
    id              CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    user_id         CHAR(36) REFERENCES users(id) ON DELETE CASCADE,
    provider        VARCHAR(50) NOT NULL, -- google, facebook, apple
    provider_id     VARCHAR(255) NOT NULL,
    provider_email  VARCHAR(255),
    created_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6),
    UNIQUE(provider, provider_id)
);