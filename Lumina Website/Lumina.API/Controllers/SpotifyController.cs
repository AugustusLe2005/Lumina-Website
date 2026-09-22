using Microsoft.AspNetCore.Mvc;
using Lumina.API.Services;

namespace Lumina.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpotifyController : ControllerBase
    {
        private readonly SpotifyService _spotifyService;

        public SpotifyController(SpotifyService spotifyService)
        {
            _spotifyService = spotifyService;
        }

        /// <summary>
        /// Lấy danh sách tất cả các thiết bị Spotify Connect đang online
        /// </summary>
        [HttpGet("devices")]
        public async Task<IActionResult> GetDevices([FromHeader(Name = "Authorization")] string? authHeader)
        {
            if (string.IsNullOrWhiteSpace(authHeader))
                return Unauthorized(new { message = "Thiếu Authorization Header!" });

            var token = authHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
            var jsonResult = await _spotifyService.GetAvailableDevicesAsync(token);
            
            return Content(jsonResult, "application/json");
        }

        /// <summary>
        /// Phát bài hát trên thiết bị Spotify Connect được chỉ định
        /// </summary>
        [HttpPut("play")]
        public async Task<IActionResult> Play(
            [FromHeader(Name = "Authorization")] string? authHeader, 
            [FromQuery] string? deviceId, 
            [FromQuery] string? trackUri)
        {
            if (string.IsNullOrWhiteSpace(authHeader))
                return Unauthorized(new { message = "Thiếu Authorization Header!" });

            if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(trackUri))
                return BadRequest(new { message = "DeviceId và TrackUri không được để trống!" });

            var token = authHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
            var success = await _spotifyService.PlayTrackAsync(token, deviceId, trackUri);
            
            return success ? Ok(new { message = "Đang phát nhạc thành công!" }) : BadRequest(new { message = "Không thể phát nhạc trên thiết bị này" });
        }
    }
}