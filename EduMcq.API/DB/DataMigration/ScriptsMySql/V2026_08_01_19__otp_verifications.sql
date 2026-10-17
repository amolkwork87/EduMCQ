CREATE TABLE IF NOT EXISTS otp_verifications (
    id              CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    user_id         CHAR(36) REFERENCES users(id) ON DELETE CASCADE,
    phone           VARCHAR(20),
    email           VARCHAR(255),
    otp_code        VARCHAR(6) NOT NULL,
    purpose         VARCHAR(50) DEFAULT 'login', -- login, verification, password_reset
    expires_at      DATETIME(6) NOT NULL,
    is_used         BOOLEAN DEFAULT false,
    attempts        INTEGER DEFAULT 0,
    created_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6)
);

CREATE INDEX idx_otp_expires_at ON otp_verifications(expires_at);
CREATE INDEX idx_otp_user_id ON otp_verifications(user_id);