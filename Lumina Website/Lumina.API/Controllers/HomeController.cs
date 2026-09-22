using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lumina.API.Models;

namespace Lumina.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly LuminaModelContext _context;

        public HomeController(LuminaModelContext context)
        {
            _context = context;
        }

        // GET: api/home
        // Lấy dữ liệu tổng hợp cho trang chủ SPA (Bài hát nổi bật, bài hát mới)
        [HttpGet]
        public async Task<IActionResult> GetHomeData()
        {
            // Lấy 10 bài hát mới nhất
            var latestSongs = await _context.BaiHats
                .OrderByDescending(b => b.Id)
                .Take(10)
                .ToListAsync();

            return Ok(new
            {
                Message = "Chào mừng đến với Lumina Music Platform!",
                Status = "Online",
                FeaturedSongs = latestSongs
            });
        }

        // GET: api/home/search?query=abc
        // Tìm kiếm nhanh bài hát, ca sĩ hoặc thể loại từ ô Search ở Header
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Ok(new List<BaiHat>());
            }

            var searchTerm = query.Trim().ToLower();

            var results = await _context.BaiHats
                .Where(b => (b.TenBaiHat != null && b.TenBaiHat.ToLower().Contains(searchTerm)) ||
                            (b.CaSi != null && b.CaSi.ToLower().Contains(searchTerm)) ||
                            (b.TheLoai != null && b.TheLoai.ToLower().Contains(searchTerm)))
                .Take(20)
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/home/ping
        // Endpoint nhẹ để Client kiểm tra trạng thái kết nối tới Server
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { timestamp = DateTime.UtcNow, status = "Lumina API is alive!" });
        }
    }
}