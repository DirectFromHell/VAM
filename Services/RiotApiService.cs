using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VAM.Services
{
    public class RiotApiService
    {
        private readonly HttpClient _httpClient;
        private string? _apiKey;
        private const string HENRIK_API = "https://api.henrikdev.xyz/valorant/v1";

        public RiotApiService(string? apiKey = null)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "V-AM/2.0");
            SetApiKey(apiKey);
        }

        public void SetApiKey(string? apiKey)
        {
            _apiKey = apiKey;
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", apiKey);
            }
        }

        /// <summary>
        /// Validate Riot ID and fetch account data
        /// </summary>
        public async Task<(bool Success, AccountData? Data, string Error)> ValidateAndFetchAccount(string riotId)
        {
            // Validate format
            if (string.IsNullOrWhiteSpace(riotId) || !riotId.Contains('#'))
            {
                return (false, null, "Invalid format. Use: Name#Tag");
            }

            var parts = riotId.Split('#');
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                return (false, null, "Invalid format. Use: Name#Tag");
            }

            string gameName = parts[0].Trim();
            string tagLine = parts[1].Trim();

            try
            {
                var url = $"{HENRIK_API}/account/{Uri.EscapeDataString(gameName)}/{Uri.EscapeDataString(tagLine)}";
                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return (false, null, $"Account '{riotId}' not found. Check the name and tag.");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    // API requires key - use basic data
                    return (true, new AccountData 
                    { 
                        Name = gameName, 
                        Tag = tagLine,
                        Region = "EU",
                        AccountLevel = 1
                    }, "warn:API key required for full data. Add key in Settings.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return (false, null, $"API error: {response.StatusCode}");
                }

                var data = JsonSerializer.Deserialize<HenrikAccountResponse>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (data?.Data == null)
                {
                    return (false, null, "Account not found");
                }

                return (true, new AccountData
                {
                    Name = data.Data.Name ?? gameName,
                    Tag = data.Data.Tag ?? tagLine,
                    Region = data.Data.Region?.ToUpper() ?? "EU",
                    AccountLevel = data.Data.AccountLevel > 0 ? data.Data.AccountLevel : 1
                }, string.Empty);
            }
            catch (HttpRequestException)
            {
                return (true, new AccountData 
                { 
                    Name = gameName, 
                    Tag = tagLine,
                    Region = "EU",
                    AccountLevel = 1
                }, "warn:Network error. Using basic data.");
            }
            catch (Exception ex)
            {
                return (false, null, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Fetch rank data
        /// </summary>
        public async Task<string?> FetchRank(string gameName, string tagLine, string region = "eu")
        {
            try
            {
                var url = $"{HENRIK_API}/mmr/{region.ToLower()}/{Uri.EscapeDataString(gameName)}/{Uri.EscapeDataString(tagLine)}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<HenrikMmrResponse>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return data?.Data?.CurrentTierPatched;
            }
            catch
            {
                return null;
            }
        }
    }

    // Data models
    public class AccountData
    {
        public string Name { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Region { get; set; } = "EU";
        public int AccountLevel { get; set; }
    }

    // Henrik API Response models
    public class HenrikAccountResponse
    {
        public int Status { get; set; }
        public HenrikAccountData? Data { get; set; }
    }

    public class HenrikAccountData
    {
        [JsonPropertyName("region")]
        public string? Region { get; set; }
        
        [JsonPropertyName("account_level")]
        public int AccountLevel { get; set; }
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("tag")]
        public string? Tag { get; set; }
    }

    public class HenrikMmrResponse
    {
        public int Status { get; set; }
        public HenrikMmrData? Data { get; set; }
    }

    public class HenrikMmrData
    {
        [JsonPropertyName("currenttierpatched")]
        public string? CurrentTierPatched { get; set; }
        
        [JsonPropertyName("ranking_in_tier")]
        public int RankingInTier { get; set; }
        
        [JsonPropertyName("elo")]
        public int Elo { get; set; }
    }
}
