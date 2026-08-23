-- ============================================================
-- Updated subscriptions table
-- ============================================================


CREATE TABLE IF NOT EXISTS subscriptions (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id                     UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    
    plan                        VARCHAR(20) NOT NULL DEFAULT 'free'
                                CHECK (plan IN ('free', 'pro', 'elite')),
    
    status                      VARCHAR(20) NOT NULL DEFAULT 'active'
                                CHECK (status IN ('active', 'cancelled', 'expired', 'pending')),
    
    -- Razorpay fields (keep for later)
    razorpay_subscription_id    VARCHAR(255) UNIQUE,
    razorpay_payment_id         VARCHAR(255),
    
    amount_paise                INTEGER,                    -- 99900 = ₹999
    currency                    VARCHAR(3) DEFAULT 'INR',
    
    started_at                  TIMESTAMPTZ NOT NULL DEFAULT now(),
    expires_at                  TIMESTAMPTZ,                -- NULL for free plan
    cancelled_at                TIMESTAMPTZ,
    auto_renew                  BOOLEAN DEFAULT true,
    
    created_at                  TIMESTAMPTZ DEFAULT now(),
    updated_at                  TIMESTAMPTZ DEFAULT now()
);

CREATE INDEX idx_subs_user          ON subscriptions(user_id);
CREATE INDEX idx_subs_user_status   ON subscriptions(user_id, status);
CREATE INDEX idx_subs_expires       ON subscriptions(expires_at) WHERE status = 'active';