using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMcq.API.Controllers;

[ApiController]
[Route("api/admin/images")]
[Authorize(Roles = "admin")]          // or [Authorize] + policy
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
        if (folder is not ("questions" or "q" or "a" or "answers"))
            return BadRequest();

        if (Path.GetFileName(fileName) != fileName)
            return BadRequest();

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg" or ".webp"))
            return BadRequest();

        var path = Path.Combine(_env.WebRootPath, "images", folder, fileName);

        if (!System.IO.File.Exists(path))
            return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(path, GetContentType(extension));
    }

    private static string GetContentType(string ext) => ext.ToLower() switch
    {
        ".png"  => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        _       => "application/octet-stream"
    };
}