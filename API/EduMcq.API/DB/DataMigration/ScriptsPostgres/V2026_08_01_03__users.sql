CREATE TABLE IF NOT EXISTS users (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    phone           VARCHAR(20) UNIQUE,
    email           VARCHAR(255) UNIQUE,
    google_id       VARCHAR(255) UNIQUE,
    facebook_id     VARCHAR(255) UNIQUE,
    apple_id        VARCHAR(255) UNIQUE, -- Added for Apple Sign-In
    name            VARCHAR(200),
    role            TEXT DEFAULT 'student',
    avatar_url      VARCHAR(500),
    is_phone_verified  BOOLEAN DEFAULT false,
    is_email_verified  BOOLEAN DEFAULT false, -- Added for email verification
    class_standard  VARCHAR(20),
    school_name     VARCHAR(300),
    target_exam_id  UUID REFERENCES exams(id) ON DELETE SET NULL,
    preferred_language VARCHAR(10) DEFAULT 'en',
    last_login_at   TIMESTAMPTZ, -- Track last login
    login_count     INTEGER DEFAULT 0, -- Track login attempts
    is_active       BOOLEAN DEFAULT true, -- Soft delete
    created_at      TIMESTAMPTZ DEFAULT now(),
    updated_at      TIMESTAMPTZ DEFAULT now()
);


CREATE INDEX idx_users_email ON users(email) WHERE email IS NOT NULL;
CREATE INDEX idx_users_phone ON users(phone) WHERE phone IS NOT NULL;
CREATE INDEX idx_users_google_id ON users(google_id) WHERE google_id IS NOT NULL;