using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lumina.API.Models;

namespace Lumina.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaiHatsController : ControllerBase
    {
        private readonly LuminaModelContext _context;

        public BaiHatsController(LuminaModelContext context)
        {
            _context = context;
        }

        // GET: api/baihats
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.BaiHats.ToListAsync();
            return Ok(list);
        }

        // GET: api/baihats/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var baiHat = await _context.BaiHats.FindAsync(id);
            if (baiHat == null)
            {
                return NotFound(new { message = "Không tìm thấy bài hát yêu cầu!" });
            }
            return Ok(baiHat);
        }

        // POST: api/baihats
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] UploadBaiHatDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Dữ liệu gửi lên rỗng!" });
                }

                string savedFileName = string.Empty;

                // Xử lý lưu file MP3 vào thư mục wwwroot/Uploads
                if (dto.FileAudio != null && dto.FileAudio.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    savedFileName = Path.GetFileName(dto.FileAudio.FileName);
                    var filePath = Path.Combine(uploadsFolder, savedFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.FileAudio.CopyToAsync(stream);
                    }
                }

                // Xử lý UrlFile để không bị null dính NOT NULL Constraint trong Database
                string finalUrlFile = !string.IsNullOrEmpty(savedFileName) ? savedFileName : dto.UrlFile;
                if (string.IsNullOrEmpty(finalUrlFile))
                {
                    finalUrlFile = "default.mp3";
                }

                var newBaiHat = new BaiHat
                {
                    TenBaiHat = !string.IsNullOrEmpty(dto.TenBaiHat) ? dto.TenBaiHat : "Bài hát mới",
                    CaSi = !string.IsNullOrEmpty(dto.CaSi) ? dto.CaSi : "Chưa rõ",
                    TheLoai = !string.IsNullOrEmpty(dto.TheLoai) ? dto.TheLoai : "Music",
                    Album = !string.IsNullOrEmpty(dto.Album) ? dto.Album : "Single",
                    Nam = (dto.Nam.HasValue && dto.Nam > 0) ? dto.Nam.Value : DateTime.Now.Year,
                    UrlFile = finalUrlFile
                };

                _context.BaiHats.Add(newBaiHat);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = newBaiHat.Id }, newBaiHat);
            }
            catch (Exception ex)
            {
                // Trả về lỗi chi tiết lên F12 Response để xem đúng nguyên nhân
                return StatusCode(500, new { 
                    message = "Lỗi Backend khi lưu bài hát", 
                    error = ex.Message, 
                    innerError = ex.InnerException?.Message ?? "Không có chi tiết phụ" 
                });
            }
        }

        // PUT: api/baihats/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateBaiHatDto dto)
        {
            var baiHat = await _context.BaiHats.FindAsync(id);
            if (baiHat == null)
            {
                return NotFound(new { message = "Không tìm thấy bài hát để cập nhật!" });
            }

            baiHat.TenBaiHat = dto.TenBaiHat;
            baiHat.CaSi = dto.CaSi;
            baiHat.TheLoai = dto.TheLoai;
            baiHat.Album = dto.Album;
            if (dto.Nam.HasValue) baiHat.Nam = dto.Nam.Value;
            if (!string.IsNullOrEmpty(dto.UrlFile)) baiHat.UrlFile = dto.UrlFile;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật bài hát thành công!", data = baiHat });
        }

        // DELETE: api/baihats/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var baiHat = await _context.BaiHats.FindAsync(id);
            if (baiHat == null)
            {
                return NotFound(new { message = "Không tìm thấy bài hát để xóa!" });
            }

            _context.BaiHats.Remove(baiHat);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa bài hát thành công!" });
        }
    }

    public class UploadBaiHatDto
    {
        public string TenBaiHat { get; set; } = string.Empty;
        public string CaSi { get; set; } = string.Empty;
        public string TheLoai { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public int? Nam { get; set; }
        public string UrlFile { get; set; } = string.Empty;
        public IFormFile? FileAudio { get; set; }
    }

    public class UpdateBaiHatDto
    {
        public string TenBaiHat { get; set; } = string.Empty;
        public string CaSi { get; set; } = string.Empty;
        public string TheLoai { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public int? Nam { get; set; }
        public string UrlFile { get; set; } = string.Empty;
    }
}