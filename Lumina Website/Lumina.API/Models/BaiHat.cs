using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lumina.API.Models
{
    [Table("BaiHat")]
    public class BaiHat
    {
        [Key]
        public int Id { get; set; }

        public string TenBaiHat { get; set; } = string.Empty;
        public string? CaSi { get; set; }
        
        // Cột lưu file MP3 bắt buộc phải là UrlFile để khớp với DB
        public string UrlFile { get; set; } = string.Empty; 

        public string? TheLoai { get; set; }
        public string? AnhBia { get; set; }
        public int? LuotNghe { get; set; }
        public string? ThoiLuong { get; set; }
        public int? Nam { get; set; }
        public string? Album { get; set; }
        public bool? IsFavorite { get; set; }
    }
}