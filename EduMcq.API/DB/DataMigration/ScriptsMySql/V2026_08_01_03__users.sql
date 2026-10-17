CREATE TABLE IF NOT EXISTS users (
    id              CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    phone           VARCHAR(20) UNIQUE,
    email           VARCHAR(255) UNIQUE,
    google_id       VARCHAR(255) UNIQUE,
    facebook_id     VARCHAR(255) UNIQUE,
    apple_id        VARCHAR(255) UNIQUE, -- Added for Apple Sign-In
    name            VARCHAR(200),
    role            VARCHAR(50) DEFAULT 'student',
    password_hash  TEXT, 
    avatar_url      VARCHAR(500),
    is_phone_verified  BOOLEAN DEFAULT false,
    is_email_verified  BOOLEAN DEFAULT false, -- Added for email verification
    class_standard  VARCHAR(20),
    school_name     VARCHAR(300),
    target_exam_id  CHAR(36) REFERENCES exams(id) ON DELETE SET NULL,
    preferred_language VARCHAR(10) DEFAULT 'en',
    last_login_at   DATETIME(6), -- Track last login
    login_count     INTEGER DEFAULT 0, -- Track login attempts
    is_active       BOOLEAN DEFAULT true, -- Soft delete
    created_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6),
    updated_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6)
);


CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_phone ON users(phone);
CREATE INDEX idx_users_google_id ON users(google_id);