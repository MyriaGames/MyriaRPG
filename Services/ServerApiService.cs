using MyriaLib.Entities.Players;
using MyriaLib.Services;
using MyriaLib.Services.Builder;
using MyriaLib.Systems;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MyriaRPG.Services
{
    public enum AuthResult { Success, InvalidCredentials, Conflict, ServerError }

    public static class ServerApiService
    {
        public static string BaseUrl { get; set; } = "http://localhost:5000";

        private static readonly HttpClient _http = new();
        private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };
        private static readonly JsonSerializerOptions _playerOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new ItemConverter() }
        };

        public static string? Token { get; private set; }
        public static string? LastUsername { get; private set; }
        public static string? LastError { get; private set; }

        public static async Task<AuthResult> LoginAsync(string username, string password)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/auth/login",
                    new { username, password });

                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                    return AuthResult.InvalidCredentials;

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    LastError = $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}";
                    return AuthResult.ServerError;
                }

                var result = await resp.Content.ReadFromJsonAsync<AuthResponse>(_jsonOpts);
                SetToken(result?.Token, result?.Username);
                return Token is not null ? AuthResult.Success : AuthResult.ServerError;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return AuthResult.ServerError;
            }
        }

        public static async Task<AuthResult> RegisterAsync(string username, string password)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/auth/register",
                    new { username, password });

                if (resp.StatusCode == HttpStatusCode.Conflict)
                    return AuthResult.Conflict;

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync();
                    LastError = $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}";
                    return AuthResult.ServerError;
                }

                var result = await resp.Content.ReadFromJsonAsync<AuthResponse>(_jsonOpts);
                SetToken(result?.Token, result?.Username);
                return Token is not null ? AuthResult.Success : AuthResult.ServerError;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return AuthResult.ServerError;
            }
        }

        public static async Task<bool> SaveCharacterAsync(Player player)
        {
            try
            {
                player.CurrentRoomId = player.CurrentRoom?.Id ?? player.CurrentRoomId;
                var dataJson = JsonSerializer.Serialize(player, _playerOpts);
                var req = new
                {
                    name          = player.Name,
                    level         = player.Level,
                    experience    = player.Experience,
                    currentRoomId = player.CurrentRoomId,
                    dataJson
                };
                var resp = await _http.PostAsJsonAsync($"{BaseUrl}/api/characters", req, _jsonOpts);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public static async Task<Player?> LoadCharacterAsync(string name)
        {
            try
            {
                var resp = await _http.GetAsync($"{BaseUrl}/api/characters/{Uri.EscapeDataString(name)}");
                if (!resp.IsSuccessStatusCode) return null;

                var dto = await resp.Content.ReadFromJsonAsync<CharacterLoadResponse>(_jsonOpts);
                if (dto is null) return null;

                var player = JsonSerializer.Deserialize<Player>(dto.DataJson, _playerOpts);
                if (player is null) return null;

                player.Level         = dto.Level;
                player.Experience    = dto.Experience;
                player.CurrentRoomId = dto.CurrentRoomId;
                player.CurrentRoom   = RoomService.AllRooms.FirstOrDefault(r => r.Id == dto.CurrentRoomId);
                player.RecalculateUnusedPoints();
                player.ValidateQuestStatuses();
                SkillFactory.UpdateSkills(player);

                return player;
            }
            catch { return null; }
        }

        public static async Task<bool> DeleteCharacterAsync(string name)
        {
            try
            {
                var resp = await _http.DeleteAsync($"{BaseUrl}/api/characters/{Uri.EscapeDataString(name)}");
                return resp.IsSuccessStatusCode || resp.StatusCode == HttpStatusCode.NoContent;
            }
            catch { return false; }
        }

        public static async Task<List<string>> GetCharacterNamesAsync()
        {
            try
            {
                var resp = await _http.GetAsync($"{BaseUrl}/api/characters");
                if (!resp.IsSuccessStatusCode) return [];
                return await resp.Content.ReadFromJsonAsync<List<string>>(_jsonOpts) ?? [];
            }
            catch { return []; }
        }

        public static void ClearToken()
        {
            Token = null;
            LastUsername = null;
            _http.DefaultRequestHeaders.Authorization = null;
        }

        private static void SetToken(string? token, string? username = null)
        {
            Token = token;
            LastUsername = username;
            _http.DefaultRequestHeaders.Authorization = token is not null
                ? new AuthenticationHeaderValue("Bearer", token)
                : null;
        }

        private record AuthResponse(string Token, string Username, DateTime ExpiresAt);
        private record CharacterLoadResponse(string DataJson, int Level, long Experience, int CurrentRoomId);
    }
}
