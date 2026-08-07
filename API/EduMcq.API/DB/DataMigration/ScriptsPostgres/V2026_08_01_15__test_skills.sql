CREATE TABLE if not exists  test_skills (
    test_id         UUID NOT NULL REFERENCES tests(id) ON DELETE CASCADE,
    skill_id        UUID NOT NULL REFERENCES thinking_skills(id) ON DELETE CASCADE,
    PRIMARY KEY (test_id, skill_id)
);