-- Exams
-- INSERT INTO exams (name, slug, description, display_order) VALUES ('SOF IMO', 'sof-imo', 'International Mathematics Olympiad', 1) ON CONFLICT (slug) DO NOTHING;
-- INSERT INTO exams (name, slug, description, display_order) VALUES ('SOF NSO', 'sof-nso', 'National Science Olympiad', 2) ON CONFLICT (slug) DO NOTHING;
-- INSERT INTO exams (name, slug, description, display_order) VALUES ('SOF NCO', 'sof-nco', 'National Cyber Olympiad', 3) ON CONFLICT (slug) DO NOTHING;
-- INSERT INTO exams (name, slug, description, display_order) VALUES ('IOQM', 'ioqm', 'Indian Olympiad Qualifier in Mathematics', 4) ON CONFLICT (slug) DO NOTHING;
INSERT INTO exams (name, slug, description, display_order) VALUES ('IPM', 'ipm', 'Institute for Promotion of Mathematics', 1) ON CONFLICT (slug) DO NOTHING;
-- INSERT INTO exams (name, slug, description, display_order) VALUES ('PRMO', 'prmo', 'Pre-Regional Mathematical Olympiad', 6) ON CONFLICT (slug) DO NOTHING;
-- INSERT INTO exams (name, slug, description, display_order) VALUES ('RMO', 'rmo', 'Regional Mathematical Olympiad', 7) ON CONFLICT (slug) DO NOTHING;
-- INSERT INTO exams (name, slug, description, display_order) VALUES ('NMTC', 'nmtc', 'National Mathematics Talent Contest', 8) ON CONFLICT (slug) DO NOTHING;