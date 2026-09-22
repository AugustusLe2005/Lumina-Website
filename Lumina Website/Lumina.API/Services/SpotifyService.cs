using System.Text.Json;
using SpotifyAPI.Web;

namespace Lumina.API.Services
{
    public class SpotifyService
    {
        /// <summary>
        /// Gọi Spotify Web API lấy danh sách thiết bị Connect
        /// </summary>
        public async Task<string> GetAvailableDevicesAsync(string accessToken)
        {
            try
            {
                var spotify = new SpotifyClient(accessToken);
                var response = await spotify.Player.GetAvailableDevices();

                // Trả về chuỗi JSON chuẩn định dạng cho Controller
                return JsonSerializer.Serialize(new { devices = response.Devices }, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
            }
            catch (APIException apiEx)
            {
                Console.WriteLine($"==> [Lumina Spotify API Error]: {apiEx.Message}");
                return JsonSerializer.Serialize(new { devices = new List<object>(), error = apiEx.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"==> [Lumina Spotify Unexpected Error]: {ex.Message}");
                return JsonSerializer.Serialize(new { devices = new List<object>(), error = ex.Message });
            }
        }

        /// <summary>
        /// Ra lệnh cho Spotify phát nhạc trên deviceId được chọn
        /// </summary>
        public async Task<bool> PlayTrackAsync(string accessToken, string? deviceId, string? trackUri)
        {
            try
            {
                var spotify = new SpotifyClient(accessToken);
                
                var request = new PlayerResumePlaybackRequest
                {
                    DeviceId = deviceId,
                    Uris = string.IsNullOrEmpty(trackUri) ? null : new List<string> { trackUri }
                };

                return await spotify.Player.ResumePlayback(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"==> [Lumina Spotify Play Error]: {ex.Message}");
                return false;
            }
        }
    }
}