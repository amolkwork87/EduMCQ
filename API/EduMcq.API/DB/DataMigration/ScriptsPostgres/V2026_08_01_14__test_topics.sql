CREATE TABLE if not exists  test_topics (
    test_id         UUID NOT NULL REFERENCES tests(id) ON DELETE CASCADE,
    topic_id        UUID NOT NULL REFERENCES topics(id) ON DELETE CASCADE,
    PRIMARY KEY (test_id, topic_id)
);