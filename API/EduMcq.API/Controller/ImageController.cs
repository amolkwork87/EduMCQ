using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/admin/images")]
[Authorize(Roles = "Admin")]          // or [Authorize] + policy
public class ImageController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public ImageController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet("{folder}/{fileName}")]
    public IActionResult GetImage(string folder, string fileName)
    {
        // Optional extra safety
        if (folder is not ("questions" or "q" or "a" or "answers"))
            return BadRequest();

        var path = Path.Combine(_env.WebRootPath, "images", folder, fileName);

        if (!System.IO.File.Exists(path))
            return NotFound();

        var contentType = GetContentType(Path.GetExtension(fileName));
        return PhysicalFile(path, contentType);
    }

    private static string GetContentType(string ext) => ext.ToLower() switch
    {
        ".png"  => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".svg"  => "image/svg+xml",
        _       => "application/octet-stream"
    };
}