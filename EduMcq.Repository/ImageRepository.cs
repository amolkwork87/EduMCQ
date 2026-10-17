using EduMcq.Interfaces;
using MySqlConnector;

namespace EduMcq.Repository;

public class ImageRepository : IImageRepository
{
    private readonly string _connectionString;

    public ImageRepository(Infrastructure.AppDb db)
    {
        _connectionString = db.ConnectionString;
    }

    public async Task InsertMetadataAsync(string publicUrl, string originalFileName, string storedFileName,
        string contentType, long fileSizeBytes, bool isQuestionImage)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new MySqlCommand(@"INSERT INTO image_library
            (image_url, original_file_name, stored_file_name, content_type, file_size_bytes,
             is_question_image, is_active, created_at)
            VALUES (@url, @originalName, @storedName, @contentType, @size, @isQuestion, TRUE, NOW())", connection);
        command.Parameters.AddWithValue("url", publicUrl);
        command.Parameters.AddWithValue("originalName", originalFileName);
        command.Parameters.AddWithValue("storedName", storedFileName);
        command.Parameters.AddWithValue("contentType", contentType);
        command.Parameters.AddWithValue("size", fileSizeBytes);
        command.Parameters.AddWithValue("isQuestion", isQuestionImage);
        await command.ExecuteNonQueryAsync();
    }
}