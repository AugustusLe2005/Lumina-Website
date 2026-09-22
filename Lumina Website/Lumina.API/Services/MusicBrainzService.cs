using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace Lumina.API.Services
{
    public class MusicBrainzMetadata
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string ReleaseYear { get; set; } = string.Empty;
        public string CoverArtUrl { get; set; } = string.Empty;
        public string MusicBrainzId { get; set; } = string.Empty;
    }

    public class MusicBrainzService
    {
        private readonly HttpClient _httpClient;

        public MusicBrainzService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "LuminaMusicServer/1.0.0 (https://github.com/lumina)");
        }

        public async Task<MusicBrainzMetadata?> EnrichYoutubeMetadataAsync(string rawTitle, string rawArtist)
        {
            string cleanTitle = CleanQuery(rawTitle);
            string cleanArtist = CleanQuery(rawArtist);

            if (cleanArtist.Equals("YouTube Stream", StringComparison.OrdinalIgnoreCase))
            {
                cleanArtist = "";
            }

            try
            {
                string query = $"recording:\"{cleanTitle}\"";
                if (!string.IsNullOrEmpty(cleanArtist))
                {
                    query += $" AND artist:\"{cleanArtist}\"";
                }

                string url = $"https://musicbrainz.org/ws/2/recording/?query={HttpUtility.UrlEncode(query)}&fmt=json&limit=1";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode) return null;

                var jsonStr = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonStr);
                var root = doc.RootElement;

                if (!root.TryGetProperty("recordings", out var recordings) || recordings.GetArrayLength() == 0)
                {
                    return null;
                }

                var rec = recordings[0];
                var result = new MusicBrainzMetadata();

                result.Title = rec.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? cleanTitle : cleanTitle;

                if (rec.TryGetProperty("artist-credit", out var artists) && artists.GetArrayLength() > 0)
                {
                    result.Artist = artists[0].TryGetProperty("name", out var artEl) ? artEl.GetString() ?? cleanArtist : cleanArtist;
                }

                if (rec.TryGetProperty("releases", out var releases) && releases.GetArrayLength() > 0)
                {
                    var firstRelease = releases[0];
                    result.Album = firstRelease.TryGetProperty("title", out var albEl) ? albEl.GetString() ?? "" : "";
                    
                    if (firstRelease.TryGetProperty("date", out var dateEl))
                    {
                        string fullDate = dateEl.GetString() ?? "";
                        result.ReleaseYear = fullDate.Length >= 4 ? fullDate.Substring(0, 4) : fullDate;
                    }

                    if (firstRelease.TryGetProperty("id", out var releaseIdEl))
                    {
                        string releaseMbid = releaseIdEl.GetString() ?? "";
                        result.MusicBrainzId = releaseMbid;

                        string caaUrl = $"https://coverartarchive.org/release/{releaseMbid}/front-500";
                        var caaCheck = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, caaUrl));
                        
                        if (caaCheck.IsSuccessStatusCode)
                        {
                            result.CoverArtUrl = caaUrl;
                        }
                    }
                }

                return result;
            }
            catch
            {
                return null;
            }
        }

        private string CleanQuery(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            string cleaned = Regex.Replace(input, @"\[.*?\]|\(.*?\)", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"OFFICIAL MUSIC VIDEO|OFFICIAL VIDEO|MUSIC VIDEO|LYRIC VIDEO|AUDIO|MV|HD|4K|CHOREOGRAPHY", "", RegexOptions.IgnoreCase);
            return cleaned.Trim();
        }
    }
}