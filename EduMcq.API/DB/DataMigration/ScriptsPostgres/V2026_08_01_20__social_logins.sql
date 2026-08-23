CREATE TABLE IF NOT EXISTS social_logins (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID REFERENCES users(id) ON DELETE CASCADE,
    provider        VARCHAR(50) NOT NULL, -- google, facebook, apple
    provider_id     VARCHAR(255) NOT NULL,
    provider_email  VARCHAR(255),
    created_at      TIMESTAMPTZ DEFAULT now(),
    UNIQUE(provider, provider_id)
);