using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace Lumina.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LyricsController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public LyricsController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "LuminaMusicApp/1.0 (https://github.com/lumina)");
        }

        [HttpGet]
        public async Task<IActionResult> GetLyrics([FromQuery] string songName, [FromQuery] string artist = "")
        {
            if (string.IsNullOrWhiteSpace(songName))
                return BadRequest(new { success = false, message = "Tên bài hát không được để trống" });

            // 1. Tách và làm sạch tên bài hát từ tiêu đề video YouTube
            var (cleanSong, cleanArtist) = ParseAndCleanTitle(songName, artist);

            try
            {
                // Cách 1: Thử Get chính xác bằng track_name + artist_name
                string lrcUrl = $"https://lrclib.net/api/get?track_name={HttpUtility.UrlEncode(cleanSong)}";
                if (!string.IsNullOrWhiteSpace(cleanArtist))
                {
                    lrcUrl += $"&artist_name={HttpUtility.UrlEncode(cleanArtist)}";
                }

                var response = await _httpClient.GetAsync(lrcUrl);
                if (response.IsSuccessStatusCode)
                {
                    var result = await ExtractLyricsFromJsonAsync(await response.Content.ReadAsStringAsync());
                    if (result != null) return Ok(result);
                }

                // Cách 2: Search mở rộng với cụm từ đã làm sạch (cleanSong + cleanArtist)
                string query1 = $"{cleanSong} {cleanArtist}".Trim();
                var search1Result = await SearchLrcLibAsync(query1);
                if (search1Result != null) return Ok(search1Result);

                // Cách 3: Search fallback chỉ bằng Tên bài hát (cleanSong)
                if (!string.IsNullOrWhiteSpace(cleanArtist))
                {
                    var search2Result = await SearchLrcLibAsync(cleanSong);
                    if (search2Result != null) return Ok(search2Result);
                }

                return Ok(new { success = false, message = "Không tìm thấy lời bài hát" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi gọi API Lời bài hát: " + ex.Message });
            }
        }

        private async Task<object?> SearchLrcLibAsync(string query)
        {
            try
            {
                string searchUrl = $"https://lrclib.net/api/search?q={HttpUtility.UrlEncode(query)}";
                var response = await _httpClient.GetAsync(searchUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonStr = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonStr);
                    var array = doc.RootElement;

                    if (array.ValueKind == JsonValueKind.Array && array.GetArrayLength() > 0)
                    {
                        foreach (var item in array.EnumerateArray())
                        {
                            string syncedLyrics = item.TryGetProperty("syncedLyrics", out var synEl) ? synEl.GetString() ?? "" : "";
                            string plainLyrics = item.TryGetProperty("plainLyrics", out var plainEl) ? plainEl.GetString() ?? "" : "";

                            if (!string.IsNullOrEmpty(syncedLyrics))
                            {
                                return new { success = true, isSynced = true, lyrics = syncedLyrics };
                            }
                            if (!string.IsNullOrEmpty(plainLyrics))
                            {
                                return new { success = true, isSynced = false, lyrics = plainLyrics };
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private async Task<object?> ExtractLyricsFromJsonAsync(string jsonStr)
        {
            using var doc = JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;

            string syncedLyrics = root.TryGetProperty("syncedLyrics", out var synEl) ? synEl.GetString() ?? "" : "";
            string plainLyrics = root.TryGetProperty("plainLyrics", out var plainEl) ? plainEl.GetString() ?? "" : "";

            if (!string.IsNullOrEmpty(syncedLyrics))
            {
                return new { success = true, isSynced = true, lyrics = syncedLyrics };
            }
            if (!string.IsNullOrEmpty(plainLyrics))
            {
                return new { success = true, isSynced = false, lyrics = plainLyrics };
            }

            return null;
        }

        private (string song, string artist) ParseAndCleanTitle(string rawSong, string rawArtist)
        {
            string song = rawSong ?? "";
            string artist = rawArtist ?? "";

            if (artist == "YouTube Stream" || artist == "YouTube Audio") artist = "";

            // Xóa ngoặc vuông/ngoặc đơn: [Official Video], (Audio), (Choreography Video)...
            song = Regex.Replace(song, @"\[.*?\]|\(.*?\)", "", RegexOptions.IgnoreCase);
            
            // Xóa ngoặc kép Nhật Bản 「...」
            song = Regex.Replace(song, @"「|」|『|』", " ");

            // Xóa các từ khóa phổ biến của YouTube
            song = Regex.Replace(song, @"OFFICIAL MUSIC VIDEO|OFFICIAL VIDEO|MUSIC VIDEO|LYRIC VIDEO|AUDIO|MV|HD|4K|CHOREOGRAPHY|PERFORMANCE VIDEO", "", RegexOptions.IgnoreCase);

            // Nếu tiêu đề dạng "YOASOBI - IDOL" hoặc "YOASOBI / アイドル"
            if (song.Contains("-") || song.Contains("–") || song.Contains("/"))
            {
                var parts = song.Split(new[] { '-', '–', '/' }, 2);
                if (parts.Length == 2)
                {
                    if (string.IsNullOrWhiteSpace(artist)) artist = parts[0].Trim();
                    song = parts[1].Trim();
                }
            }

            return (song.Trim(), artist.Trim());
        }
    }
}