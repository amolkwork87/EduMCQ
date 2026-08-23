using System.Data;
using DotNetEnv;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.FileProviders;
using Npgsql;


// Load environment variables from .env file
Env.Load();
var builder = WebApplication.CreateBuilder(args);

var uploadRoot = Path.Combine(builder.Environment.WebRootPath ?? "wwwroot", "images");
Directory.CreateDirectory(Path.Combine(uploadRoot, "questions")); // question images
Directory.CreateDirectory(Path.Combine(uploadRoot, "answers")); // answer images


// 1. Load configuration in order (last one wins)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
    
builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        
        // policy.WithOrigins("http://localhost:5173")
        // .AllowAnyHeader()
        //       .AllowAnyMethod()
        //       .AllowCredentials();
              //.SetIsOriginAllowed(_ => true);

              policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None; // required for cross-domain
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only
});


// Repositories
builder.Services.AddRepositories();
builder.Services.AddServices();
builder.Services.JWTAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

var app = builder.Build();


app.UseCors("AllowFrontend"); 

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.ApplyDbMigrations(builder.Configuration["EduMcqDatabaseConnection"]);

// app.UseHttpsRedirection();
app.UseAuthentication(); 
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();

app.Run();

