using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using Lumina.API.Models;
using Lumina.API;
using Lumina.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký HttpClient cho SpotifyService
builder.Services.AddHttpClient<SpotifyService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<MusicBrainzService>();

// 1. Đăng ký DbContext kết nối SQL Server
builder.Services.AddDbContext<LuminaModelContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Dịch vụ Controller API & Cấu hình JSON chuẩn camelCase cho SPA Frontend
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// 3. Cấu hình Authentication Middleware (Cookies cho Social OAuth2 + JWT Bearer)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
})
.AddGitHub(options =>
{
    options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"]!;
})
.AddTwitter(options =>
{
    options.ClientId = builder.Configuration["Authentication:X:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:X:ClientSecret"]!;
});

// 4. Cấu hình CORS cho SPA Frontend Lumina
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSPA", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowSPA");
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

// Bật Middleware Xác thực & Phân quyền (Đặt đúng thứ tự trước MapControllers)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();