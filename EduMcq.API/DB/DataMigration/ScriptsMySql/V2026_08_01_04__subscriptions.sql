-- ============================================================
-- Updated subscriptions table
-- ============================================================


CREATE TABLE IF NOT EXISTS subscriptions (
    id                          CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    user_id                     CHAR(36) NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    
    plan                        VARCHAR(20) NOT NULL DEFAULT 'free'
                                CHECK (plan IN ('free', 'pro', 'elite')),
    
    status                      VARCHAR(20) NOT NULL DEFAULT 'active'
                                CHECK (status IN ('active', 'cancelled', 'expired', 'pending')),
    
    -- Razorpay fields (keep for later)
    razorpay_subscription_id    VARCHAR(255) UNIQUE,
    razorpay_payment_id         VARCHAR(255),
    
    amount_paise                INTEGER,                    -- 99900 = ₹999
    currency                    VARCHAR(3) DEFAULT 'INR',
    
    started_at                  DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    expires_at                  DATETIME(6),                -- NULL for free plan
    cancelled_at                DATETIME(6),
    auto_renew                  BOOLEAN DEFAULT true,
    
    created_at                  DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6),
    updated_at                  DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6)
);

CREATE INDEX idx_subs_user          ON subscriptions(user_id);
CREATE INDEX idx_subs_user_status   ON subscriptions(user_id, status);
CREATE INDEX idx_subs_expires       ON subscriptions(expires_at);