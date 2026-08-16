CREATE TABLE if not exists  guest_responses (
    id              UUID PRIMARY KEY,
    device_uuid     VARCHAR(100) NOT NULL,
    question_id     UUID NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    selected_option_id UUID REFERENCES options(id) ON DELETE SET NULL,
    time_spent_sec  INTEGER DEFAULT 0,
    created_at      TIMESTAMPTZ DEFAULT now()
);
CREATE INDEX idx_guest_device ON guest_responses(device_uuid);