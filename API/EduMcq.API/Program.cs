using System.Data;
using Microsoft.AspNetCore.Cors;
using Npgsql;
using Microsoft.Extensions.FileProviders;


// Load environment variables from .env file
//Env.Load();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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

// builder.Services.AddScoped<IDbConnection>(sp =>
// {
//     var conn = new NpgsqlConnection(
//         builder.Configuration.GetConnectionString("DefaultConnection"));
//     conn.Open();
//     return conn;
// });



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

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");


app.ApplyDbMigrations(builder.Configuration["EduMcqDatabaseConnection"]);


// app.UseHttpsRedirection();

// app.UseAuthentication();   // ← Must come BEFORE UseAuthorization
// app.UseAuthorization();

app.MapControllers();

app.Run();
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
