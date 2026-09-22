using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Lumina.API.Models;

namespace Lumina.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly LuminaModelContext _context;
        private readonly IConfiguration _config;

        // Constructor Injection cho DbContext và Configuration
        public AccountController(LuminaModelContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // POST: api/Account/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
                {
                    return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ thông tin!" });
                }

                string inputUser = model.Username.Trim().ToLower();
                string inputPass = model.Password.Trim();

                var user = _context.Accounts
                    .AsEnumerable()
                    .FirstOrDefault(a => a.Username != null && a.Username.Trim().ToLower() == inputUser 
                                      && a.Password != null && a.Password.Trim() == inputPass);

                if (user == null)
                {
                    return Unauthorized(new { success = false, message = "Tài khoản hoặc mật khẩu không chính xác!" });
                }

                // Sinh mã JWT Token thực tế
                string token = GenerateJwtToken(user);

                return Ok(new
                {
                    success = true,
                    message = "Đăng nhập thành công!",
                    token = token,
                    user = new
                    {
                        id = user.Id,
                        username = user.Username,
                        fullName = user.FullName ?? user.Username,
                        role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi Server: " + ex.Message });
            }
        }

        // POST: api/Account/register
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterDto model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
                {
                    return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ thông tin!" });
                }

                string inputUser = model.Username.Trim().ToLower();

                var existingUser = _context.Accounts
                    .AsEnumerable()
                    .FirstOrDefault(a => a.Username != null && a.Username.Trim().ToLower() == inputUser);

                if (existingUser != null)
                {
                    return BadRequest(new { success = false, message = "Tên tài khoản này đã được sử dụng!" });
                }

                var newUser = new Account
                {
                    Username = model.Username.Trim(),
                    Password = model.Password.Trim(),
                    FullName = string.IsNullOrWhiteSpace(model.FullName) ? model.Username.Trim() : model.FullName.Trim(),
                    Role = "User"
                };

                _context.Accounts.Add(newUser);
                _context.SaveChanges();

                return Ok(new { success = true, message = "Đăng ký tài khoản thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi Server: " + ex.Message });
            }
        }

        // GET: api/Account/external-login?provider=Google
        [HttpGet("external-login")]
        public IActionResult ExternalLogin([FromQuery] string provider)
        {
            if (string.IsNullOrEmpty(provider))
            {
                return BadRequest(new { success = false, message = "Phương thức đăng nhập không hợp lệ!" });
            }

            var redirectUrl = Url.Action("ExternalLoginCallback", "Account");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        // GET: api/Account/external-callback
        [HttpGet("external-callback")]
        public async Task<IActionResult> ExternalLoginCallback()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded)
            {
                return Redirect("/login.html?error=auth_failed");
            }

            var claims = authenticateResult.Principal.Claims;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value 
                       ?? claims.FirstOrDefault(c => c.Type == "urn:github:login")?.Value 
                       ?? "LuminaMember";

            var username = email ?? name.Replace(" ", "").ToLower();

            // Tìm tài khoản trong DB, nếu chưa có thì tự động tạo record mới
            var user = _context.Accounts.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                user = new Account
                {
                    Username = username,
                    Password = "OAuthExternalUserPass2026",
                    FullName = name,
                    Role = "User"
                };
                _context.Accounts.Add(user);
                _context.SaveChanges();
            }

            // Sinh JWT Token
            string jwtToken = GenerateJwtToken(user);

            // Script tự động lưu Token và User vào LocalStorage rồi nhảy về trang chủ
            var htmlContent = $@"
                <html>
                <body>
                    <script>
                        var userObj = {{
                            id: {user.Id},
                            username: '{user.Username}',
                            fullName: '{user.FullName}'
                        }};
                        localStorage.setItem('user', JSON.stringify(userObj));
                        localStorage.setItem('currentUser', JSON.stringify(userObj));
                        localStorage.setItem('token', '{jwtToken}');
                        localStorage.setItem('jwt_token', '{jwtToken}');
                        window.location.href = '/index.html';
                    </script>
                </body>
                </html>";

            return Content(htmlContent, "text/html");
        }

        // Hàm helper sinh JWT Token chuẩn
        private string GenerateJwtToken(Account user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _config["Jwt:Key"] ?? "LuminaCherrySecretKey2026SuperSecureKeyKey!";
            var key = Encoding.UTF8.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username ?? ""),
                    new Claim("FullName", user.FullName ?? user.Username ?? "")
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}