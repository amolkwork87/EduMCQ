CREATE TABLE IF NOT EXISTS image_library (
    id                  CHAR(36) PRIMARY KEY DEFAULT (UUID()),
    image_url           VARCHAR(500) NOT NULL,
    original_file_name  VARCHAR(260),
    stored_file_name    VARCHAR(100),
    content_type        VARCHAR(100),
    file_size_bytes     BIGINT,
    is_question_image   BOOLEAN NOT NULL DEFAULT TRUE,  -- true = question, false = option
    uploaded_by         VARCHAR(100),
    is_active           BOOLEAN DEFAULT TRUE,
    created_at          DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6)
);

CREATE INDEX idx_image_library_active ON image_library(is_active);