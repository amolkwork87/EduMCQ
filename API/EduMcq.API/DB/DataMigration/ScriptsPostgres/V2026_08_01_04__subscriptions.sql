CREATE TABLE if not exists  subscriptions (
    id              UUID PRIMARY KEY,
    user_id         UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    plan            TEXT DEFAULT 'monthly',
    razorpay_subscription_id VARCHAR(255) UNIQUE,
    razorpay_payment_id VARCHAR(255),
    status          TEXT DEFAULT 'active',
    amount_paise    INTEGER,
    currency        VARCHAR(3) DEFAULT 'INR',
    started_at      TIMESTAMPTZ NOT NULL,
    expires_at      TIMESTAMPTZ NOT NULL,
    auto_renew      BOOLEAN DEFAULT true,
    created_at      TIMESTAMPTZ DEFAULT now()
);
CREATE INDEX idx_subs_user ON subscriptions(user_id);
CREATE INDEX idx_subs_status ON subscriptions(user_id, status);