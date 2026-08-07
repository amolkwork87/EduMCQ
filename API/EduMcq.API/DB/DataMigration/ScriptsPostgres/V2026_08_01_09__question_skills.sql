CREATE TABLE if not exists  question_skills (
    question_id     UUID NOT NULL REFERENCES questions(id) ON DELETE CASCADE,
    skill_id        UUID NOT NULL REFERENCES thinking_skills(id) ON DELETE CASCADE,
    PRIMARY KEY (question_id, skill_id)
);