using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Lumina.API.Models;

// =========================================================================
// HÀM TỰ ĐỌC FILE .ENV VÀ NẠP VÀO ENVIRONMENT VARIABLES (SIÊU CHUẨN DỄ DÙNG)
// =========================================================================
var envFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envFilePath))
{
    foreach (var line in File.ReadAllLines(envFilePath))
    {
        var parts = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// Lấy giá trị trực tiếp từ Environment Variables
string GetEnv(string key, string defaultValue = "") 
    => Environment.GetEnvironmentVariable(key) ?? builder.Configuration[key] ?? defaultValue;

// =========================================================================
// 1. CẤU HÌNH DATABASE
// =========================================================================
var connectionString = GetEnv("DefaultConnection", 
    "Server=(localdb)\\MSSQLLocalDB;Database=WebMusicDB;Trusted_Connection=True;TrustServerCertificate=True;");

builder.Services.AddDbContext<LuminaModelContext>(options =>
    options.UseSqlServer(connectionString));

// =========================================================================
// 2. CẤU HÌNH AUTHENTICATION (ĐỌC TRỰC TIẾP TỪ .ENV)
// =========================================================================
var jwtSecretKey = GetEnv("JWT_KEY", "LuminaCherrySecretKey2026SuperSecureKeyKey!");
var key = Encoding.ASCII.GetBytes(jwtSecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddCookie()
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
})
// 1. GOOGLE OAUTH
.AddGoogle("Google", options =>
{
    options.ClientId = GetEnv("GOOGLE_CLIENT_ID", "dummy-google-client-id");
    options.ClientSecret = GetEnv("GOOGLE_CLIENT_SECRET", "dummy-google-client-secret");
})
// 2. X / TWITTER OAUTH
.AddTwitter("Twitter", options =>
{
    options.ClientId = GetEnv("X_CLIENT_ID", "dummy-x-client-id");
    options.ClientSecret = GetEnv("X_CLIENT_SECRET", "dummy-x-client-secret");
})
// 3. GITHUB OAUTH
.AddGitHub("GitHub", options =>
{
    options.ClientId = GetEnv("GITHUB_CLIENT_ID", "dummy-github-client-id");
    options.ClientSecret = GetEnv("GITHUB_CLIENT_SECRET", "dummy-github-client-secret");
});

// =========================================================================
// 3. CẤU HÌNH AUTHORIZATION POLICIES
// =========================================================================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireArtist", policy => policy.RequireRole("Artist", "Admin"));
    options.AddPolicy("RequireUser", policy => policy.RequireRole("User", "Artist", "Admin"));
});

// =========================================================================
// 4. CẤU HÌNH RATE LIMITING (Bảo vệ GPU / MI50)
// =========================================================================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("AiChatPolicy", httpContext =>
    {
        var username = httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString();
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: username,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            });
    });
});

// =========================================================================
// 5. CẤU HÌNH HTTP CLIENT CHO OLLAMA API
// =========================================================================
builder.Services.AddHttpClient("OllamaClient", client =>
{
    var ollamaUrl = GetEnv("OLLAMA_URL", "http://192.168.1.100:11434/");
    client.BaseAddress = new Uri(ollamaUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

// =========================================================================
// 6. CẤU HÌNH CORS, CONTROLLERS & SWAGGER
// =========================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("LuminaCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// =========================================================================
// 7. MIDDLEWARE PIPELINE
// =========================================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("LuminaCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();