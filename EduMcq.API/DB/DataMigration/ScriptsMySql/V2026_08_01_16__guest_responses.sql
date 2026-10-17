CREATE TABLE if not exists  guest_responses (
    id              CHAR(36) PRIMARY KEY,
    device_uuid     VARCHAR(100) NOT NULL,
    question_id     CHAR(36) NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    selected_option_id CHAR(36) REFERENCES options(id) ON DELETE SET NULL,
    time_spent_sec  INTEGER DEFAULT 0,
    created_at      DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6)
);
CREATE INDEX idx_guest_device ON guest_responses(device_uuid);