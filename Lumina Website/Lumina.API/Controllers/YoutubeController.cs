using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Lumina.API.Services;

namespace Lumina.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class YoutubeController : ControllerBase
    {
        private readonly string _ytDlpPath;
        private readonly MusicBrainzService _mbService;

        public YoutubeController(IWebHostEnvironment env, MusicBrainzService mbService)
        {
            _ytDlpPath = Path.Combine(env.ContentRootPath, "yt-dlp.exe");
            _mbService = mbService;
        }

        // POST: api/youtube/info
        [HttpPost("info")]
        public async Task<IActionResult> GetVideoInfo([FromBody] YoutubeRequest req)
        {
            if (string.IsNullOrEmpty(req.Url))
                return BadRequest(new { message = "URL không hợp lệ" });

            string videoId = ExtractYoutubeId(req.Url);

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = System.IO.File.Exists(_ytDlpPath) ? _ytDlpPath : "yt-dlp",
                    Arguments = $"--print \"%(title)s|||%(uploader)s\" --no-warnings --referer \"https://www.youtube.com/\" --user-agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36\" \"{req.Url}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using var process = new Process { StartInfo = psi };
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                string title = "YouTube Audio";
                string artist = "YouTube Stream";

                if (!string.IsNullOrWhiteSpace(output))
                {
                    var parts = output.Trim().Split(new[] { "|||" }, StringSplitOptions.None);
                    if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0])) title = parts[0].Trim();
                    if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])) artist = parts[1].Trim();
                }

                string thumbnailUrl = !string.IsNullOrEmpty(videoId) 
                    ? $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg" 
                    : "";

                string album = "";

                if (_mbService != null)
                {
                    var mbData = await _mbService.EnrichYoutubeMetadataAsync(title, artist);
                    if (mbData != null)
                    {
                        if (!string.IsNullOrWhiteSpace(mbData.Title)) title = mbData.Title;
                        if (!string.IsNullOrWhiteSpace(mbData.Artist)) artist = mbData.Artist;
                        if (!string.IsNullOrWhiteSpace(mbData.Album)) album = mbData.Album;
                        if (!string.IsNullOrWhiteSpace(mbData.CoverArtUrl)) thumbnailUrl = mbData.CoverArtUrl;
                    }
                }

                return Ok(new { id = videoId, title, artist, album, thumbnailUrl });
            }
            catch
            {
                return Ok(new { 
                    id = videoId, 
                    title = "YouTube Track", 
                    artist = "YouTube Stream", 
                    album = "",
                    thumbnailUrl = !string.IsNullOrEmpty(videoId) ? $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg" : "" 
                });
            }
        }

        // POST: api/youtube/stream
        [HttpPost("stream")]
        public async Task<IActionResult> GetStreamUrl([FromBody] YoutubeRequest req)
        {
            if (string.IsNullOrEmpty(req.Url))
                return BadRequest(new { message = "URL không hợp lệ" });

            try
            {
                // Bổ sung User-Agent, Referer và nới lỏng Format filter để yt-dlp lấy stream mượt nhất
                string args = $"-g -f bestaudio/ba/best --no-warnings --referer \"https://www.youtube.com/\" --user-agent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36\" \"{req.Url}\"";

                var psi = new ProcessStartInfo
                {
                    FileName = System.IO.File.Exists(_ytDlpPath) ? _ytDlpPath : "yt-dlp",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using var process = new Process { StartInfo = psi };
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!string.IsNullOrWhiteSpace(output))
                {
                    string directUrl = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                    return Ok(new { streamUrl = directUrl.Trim() });
                }

                return BadRequest(new { message = "Không lấy được direct stream từ YouTube: " + error });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi thực thi yt-dlp: " + ex.Message });
            }
        }

        private string ExtractYoutubeId(string url)
        {
            var match = Regex.Match(url, @"(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=))([\w-]{11})");
            return match.Success ? match.Groups[1].Value : "";
        }
    }

    public class YoutubeRequest
    {
        public string Url { get; set; } = string.Empty;
    }
}