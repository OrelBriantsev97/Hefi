using System.Data;
using System.Security.Claims;
using System.Text;
using Dapper;
using Npgsql;
using Hefi.Api;
using Hefi.Api.Endpoints;
using Hefi.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Maui.Controls;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddAzureWebAppDiagnostics();

// Configuration: Postgres connection string
// Keys checked (first match wins):
// 1) ConnectionStrings:Postgres (appsettings.json / environment)
// 2) ConnectionStrings__Postgres (flattened env var style)
// 3) CUSTOMCONNSTR_Postgres (Azure App Settings "Connection strings")
// 4) POSTGRESQLCONNSTR_Postgres (Azure alternate key)
var connString = builder.Configuration.GetConnectionString("Postgres")
     ?? builder.Configuration["ConnectionStrings__Postgres"]
    ?? Environment.GetEnvironmentVariable("CUSTOMCONNSTR_Postgres")
    ?? Environment.GetEnvironmentVariable("POSTGRESQLCONNSTR_Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' not found.");

builder.Services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(connString));

// JWT Authentication -  Expected configuration:
//   Jwt:Key = <long random secret>, Jwt:Issuer = Hefi 
//   Jwt:Audience = HefiUsers , Jwt:ExpiresHours = 2

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
var jwtIssuer = jwtSection["Issuer"] ?? "Hefi";
var jwtAudience = jwtSection["Audience"] ?? "HefiUsers";
var jwtExpiresHours = int.TryParse(jwtSection["ExpiresHours"], out var h) ? h : 2;



builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// - Swagger -
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token.\nExample: Bearer eyJhbGciOiJIUzI1NiIs..."
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpClient<FoodSearchService>();

// -CORS for future frontend app development- TODO: check if needed
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .WithOrigins("http://localhost"));
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

/// returns true if the request contains the correct header.
static bool IsAdmin(HttpContext ctx, IConfiguration cfg)
{
    var expected = cfg["ADMIN_INIT_KEY"]; // set in Azure App Settings
    if (string.IsNullOrEmpty(expected)) return false;
    var provided = ctx.Request.Headers["X-Admin-Key"].ToString();
    return string.Equals(provided, expected, StringComparison.Ordinal);
}
/// extracts user's ID .throws if missing/invalid
static int GetUserId(ClaimsPrincipal user)
{
    var val = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(val)) throw new InvalidOperationException("User id claim missing");
    return int.Parse(val);
}

// health : GET /health
app.MapGet("health", () =>
    Results.Ok(new { status = "ok", name = "Hefi API", time = DateTime.UtcNow }));

// init : GET/init with header X-Admin-Key: <ADMIN_INIT_KEY>
app.MapGet("/_init", async (HttpContext ctx, IConfiguration cfg, IDbConnection db, ILoggerFactory lf) =>
{
    if (!IsAdmin(ctx, cfg)) return Results.Unauthorized();

    try
    {
        await Database.InitAsync(db);
        return Results.Ok(new { status = "ok", message = "Database initialized" });
    }
    catch (Exception ex)
    {
        lf.CreateLogger("Init").LogError(ex, "Init failed");
        return Results.Problem($"Init failed: {ex.Message}", statusCode: 500);
    }
})
.WithMetadata(new ApiExplorerSettingsAttribute { IgnoreApi = true });

// Daily Summary : GET /summary/{date} (authorized)
app.MapGet("/summary/{date}", async (ClaimsPrincipal user, IDbConnection db, DateTime date) =>
{
    var userId = GetUserId(user);

    const string sql = """
        SELECT
            COALESCE(SUM(total_kcal), 0)    AS kcal,
            COALESCE(SUM(total_protein), 0) AS protein,
            COALESCE(SUM(total_carbs), 0)   AS carbs,
            COALESCE(SUM(total_fat), 0)     AS fat,
            COALESCE(SUM(total_sugar), 0)   AS sugar
        FROM meals
        WHERE user_id = @userId
          AND DATE(eaten_at) = @date;
    """;

    var summary = await db.QuerySingleAsync(sql, new { userId, date });
    return Results.Ok(summary);
})
.RequireAuthorization();


// ---------- Endpoints ----------
app.MapUserEndpoints();
app.MapAuthEndpoints(builder.Configuration);
app.MapMealEndpoints();
app.MapWorkoutEndpoints();
app.MapFoodEndpoints();

app.Run();
