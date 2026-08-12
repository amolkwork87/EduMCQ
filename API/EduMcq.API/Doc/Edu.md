exams (1)
  ├── standards (N)          [Class 10, 11, 12 under JEE Main]
  ├── subjects (N)           [Physics, Chemistry, Math under JEE Main]
  │     └── topics (N)       [Trigonometry → Heights & Distances (tree)]
  │           └── concepts (N) [Angle of Elevation under Heights & Distances]
  │
  └── questions (N)
        ├── options (4 per question)
        ├── question_topics (M:N)
        ├── question_skills (M:N)
        └── question_images (N)

thinking_skills (global lookup) → question_skills (M:N)



=== Table====
**Master**          | exams, standards, subjects, topics, thinking_skills, concepts
**Junction**        | standard_subjects, topic_standards, question_topics, question_skills,test_topics, test_skills
**Content**         | questions, options, question_images, question_versions
**Test Definition** | tests, test_questions
**Responses**       | user_responses, guest_responses, user_skill_analytics


=== Two Test Modes
A: Ad-hoc Practice | User picks Exam→Standard→Subject, gets 10 random questions | questions → question_topics →     topic_standards → options

B:  Pre-built Test | Admin creates fixed test, user takes by test ID | tests → test_questions → questions → options





