using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lumina.API.Models;

namespace Lumina.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly LuminaModelContext _context = new LuminaModelContext();

        public AdminController(LuminaModelContext context)
        {
            _context = context;
        }

        // GET: api/admin/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var totalSongs = await _context.BaiHats.CountAsync();
            var totalUsers = await _context.Accounts.CountAsync(); // Dùng Accounts khớp với DbSet trong LuminaDbContext

            return Ok(new
            {
                TotalSongs = totalSongs,
                TotalUsers = totalUsers,
                SystemStatus = "Active"
            });
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Accounts.Select(a => new 
            {
                a.Id,
                a.Username,
                a.Role
            }).ToListAsync();

            return Ok(users);
        }
    }
}