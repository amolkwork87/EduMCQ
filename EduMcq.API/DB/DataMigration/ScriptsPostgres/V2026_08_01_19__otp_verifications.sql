CREATE TABLE IF NOT EXISTS otp_verifications (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID REFERENCES users(id) ON DELETE CASCADE,
    phone           VARCHAR(20),
    email           VARCHAR(255),
    otp_code        VARCHAR(6) NOT NULL,
    purpose         VARCHAR(50) DEFAULT 'login', -- login, verification, password_reset
    expires_at      TIMESTAMPTZ NOT NULL,
    is_used         BOOLEAN DEFAULT false,
    attempts        INTEGER DEFAULT 0,
    created_at      TIMESTAMPTZ DEFAULT now()
);

CREATE INDEX idx_otp_expires_at ON otp_verifications(expires_at) WHERE is_used = false;
CREATE INDEX idx_otp_user_id ON otp_verifications(user_id);