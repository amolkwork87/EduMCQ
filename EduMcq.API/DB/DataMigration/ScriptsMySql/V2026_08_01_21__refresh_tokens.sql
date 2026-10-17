

CREATE TABLE IF NOT EXISTS refresh_tokens (
    id              CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    user_id         CHAR(36) NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    
    -- The actual token hash (never store raw tokens)
    token_hash      VARCHAR(255) NOT NULL UNIQUE,
    
    -- Metadata
    issued_at       DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    expires_at      DATETIME(6) NOT NULL,
    revoked_at      DATETIME(6),
    
    -- Rotation tracking (prevents replay attacks)
    replaced_by_token_hash VARCHAR(255),
    
    -- Device/session context (optional but useful)
    device_info     VARCHAR(255),
    ip_address      VARCHAR(45),
    
    -- Constraints
    CONSTRAINT refresh_tokens_not_revoked_and_expired CHECK (
        revoked_at IS NULL OR revoked_at >= issued_at
    )
);

-- Indexes
CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX idx_refresh_tokens_token_hash ON refresh_tokens(token_hash);
CREATE INDEX idx_refresh_tokens_expires_at ON refresh_tokens(expires_at);