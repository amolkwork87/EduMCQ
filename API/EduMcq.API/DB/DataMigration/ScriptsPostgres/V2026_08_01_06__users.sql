CREATE TABLE if not exists  users (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    phone           VARCHAR(20) UNIQUE,
    email           VARCHAR(255) UNIQUE,
    google_id       VARCHAR(255) UNIQUE,
    facebook_id     VARCHAR(255) UNIQUE,
    name            VARCHAR(200),
    role            user_role DEFAULT 'student',
    avatar_url      VARCHAR(500),
    is_phone_verified  BOOLEAN DEFAULT false,
    class_standard  VARCHAR(20),
    school_name     VARCHAR(300),
    target_exam_id  UUID REFERENCES exams(id) ON DELETE SET NULL,
    preferred_language VARCHAR(10) DEFAULT 'en',
    created_at      TIMESTAMPTZ DEFAULT now(),
    updated_at      TIMESTAMPTZ DEFAULT now()
);