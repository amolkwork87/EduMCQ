// // ============================================================
//     // HIERARCHY BUILDER (Get full tree for a given exam)
//     // ============================================================
//     [HttpGet("hierarchy/{examId:guid}")]
//     public async Task<IActionResult> GetHierarchy(Guid examId)
//     {
//         await using var conn = new MySqlConnection(_connectionString);
//         await conn.OpenAsync();

//         var hierarchy = new HierarchyResponse();

//         // Get exam
//         await using (var cmd = new MySqlCommand("SELECT * FROM exams WHERE id = @id", conn))
//         {
//             cmd.Parameters.AddWithValue("id", examId);
//             await using var reader = await cmd.ExecuteReaderAsync();
//             if (await reader.ReadAsync())
//                 hierarchy.Exam = MapExam(reader);
//         }

//         // Get standards
//         hierarchy.Standards = new List<Standard>();
//         await using (var cmd = new MySqlCommand("SELECT * FROM standards WHERE exam_id = @id AND is_active = true ORDER BY display_order", conn))
//         {
//             cmd.Parameters.AddWithValue("id", examId);
//             await using var reader = await cmd.ExecuteReaderAsync();
//             while (await reader.ReadAsync())
//                 hierarchy.Standards.Add(MapStandard(reader));
//         }

//         // Get subjects with topics tree
//         hierarchy.Subjects = new List<SubjectWithTopics>();
//         await using (var cmd = new MySqlCommand(@"
//             SELECT s.*, 
//                 (SELECT json_agg(json_build_object(
//                     'id', t.id, 'name', t.name, 'slug', t.slug, 
//                     'parentId', t.parent_id, 'depthLevel', t.depth_level,
//                     'concepts', (SELECT json_agg(json_build_object('id', c.id, 'name', c.name, 'slug', c.slug))
//                                  FROM concepts c WHERE c.topic_id = t.id AND c.is_active = true)
//                 ) ORDER BY t.display_order, t.name)
//                 FROM topics t WHERE t.subject_id = s.id AND t.is_active = true AND t.parent_id IS NULL) as topics
//             FROM subjects s WHERE s.exam_id = @id AND s.is_active = true ORDER BY s.display_order", conn))
//         {
//             cmd.Parameters.AddWithValue("id", examId);
//             await using var reader = await cmd.ExecuteReaderAsync();
//             while (await reader.ReadAsync())
//             {
//                 var subject = MapSubject(reader);
//                 var topicsJson = reader.IsDBNull(7) ? "[]" : reader.GetString(7);
//                 hierarchy.Subjects.Add(new SubjectWithTopics 
//                 { 
//                     Subject = subject, 
//                     Topics = System.Text.Json.JsonSerializer.Deserialize<List<TopicWithConcepts>>(topicsJson) ?? new()
//                 });
//             }
//         }

//         return Ok(hierarchy);
//     }