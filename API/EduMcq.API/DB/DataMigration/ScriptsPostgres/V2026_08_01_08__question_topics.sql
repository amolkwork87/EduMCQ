CREATE TABLE if not exists  question_topics (
    question_id     UUID NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    topic_id        UUID NOT NULL REFERENCES topics(id) ON DELETE CASCADE,
    PRIMARY KEY (question_id, topic_id)
);