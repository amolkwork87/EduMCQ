using System.Text;
using EduMcq.Interfaces;
using EduMcq.Repository;
using EduMcq.Services;
using EvolveDb;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

public static class ServiceCollection
{

//     public static void AddSwaggerConfiguration(this IServiceCollection services)
//     {
//         services.AddSwaggerGen(c =>
// {
//     c.SwaggerDoc("v1", new OpenApiInfo { Title = "EduMCQ API", Version = "v1" });

//     c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//     {
//         Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
//         Name = "Authorization",
//         In = ParameterLocation.Header,
//         Type = SecuritySchemeType.ApiKey,
//         Scheme = "Bearer"
//     });

//     c.AddSecurityRequirement(new OpenApiSecurityRequirement
//     {
//         {
//             new OpenApiSecurityScheme
//             {
//                 Reference = new OpenApiReference
//                 {
//                     Type = ReferenceType.SecurityScheme,
//                     Id = "Bearer"
//                 }
//             },
//             Array.Empty<string>()
//         }
//     });
// });

//     }
    public static void ApplyDbMigrations(this IApplicationBuilder app, string connectionString)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DBMigration");
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        var evolve = new Evolve(connection, msg => logger.LogInformation(msg))
        {
            Locations = new[] { "DB/DataMigration" },
            IsEraseDisabled = true,
            MetadataTableName = "edumcq_changelog",
            OutOfOrder = true //Comment in production
        };

        evolve.Migrate();
    }

    public static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IExamRepository, ExamRepository>();
        services.AddScoped<IConceptRepository, ConceptRepository>();
        services.AddScoped<ITopicRepository, TopicRepository>();
        services.AddScoped<IStandardRepository, StandardRepository>();
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IExcelImportRepository, ExcelImportRepository>();
        services.AddScoped<IImageRepository, ImageRepository>();
        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<IProgressRepository, ProgressRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
    }

    public static void AddServices(this IServiceCollection services)
    {

        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddScoped<IImageImportService, ImageImportService>();
        services.AddScoped<IExamService, ExamService>();
        services.AddScoped<IConceptService, ConceptService>();
        services.AddScoped<ITopicService, TopicService>();
        services.AddScoped<IStandardService, StandardService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<OtpService>();
        services.AddScoped<JwtService>();
        services.AddScoped<SocialLoginService>();
        services.AddScoped<UserService>();
         services.AddScoped<ProgressService>();
         services.AddScoped<SubscriptionService>();
         services.AddScoped<ArticleService>();

    }



    public static void JWTAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<JwtService>();
        var jwtSettings = configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
        services.AddAuthentication(options =>
        {

            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),

        // Optional: Clock skew (useful in development)
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Add("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };
});

    }
}