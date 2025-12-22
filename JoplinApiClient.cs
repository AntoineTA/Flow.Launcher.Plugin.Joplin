using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.Joplin
{
    public class JoplinApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly Settings _settings;

        public JoplinApiClient(Settings settings)
        {
            _settings = settings;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        private string GetApiUrl(string endpoint)
        {
            var baseUrl = $"http://localhost:{_settings.GetPort()}";
            var separator = endpoint.Contains("?") ? "&" : "?";
            return $"{baseUrl}/{endpoint}{separator}token={_settings.ApiToken}";
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(GetApiUrl("ping"));
                var content = await response.Content.ReadAsStringAsync();
                return content == "JoplinClipperServer";
            }
            catch
            {
                return false;
            }
        }

        public async Task<JoplinNote?> CreateNoteAsync(string title, string body, string? notebookId = null)
        {
            try
            {
                var data = new Dictionary<string, string>
                {
                    { "title", title },
                    { "body", body }
                };

                if (!string.IsNullOrEmpty(notebookId))
                {
                    data["parent_id"] = notebookId;
                }
                else if (!string.IsNullOrEmpty(_settings.DefaultNotebookName))
                {
                    var defaultNotebookId = await GetNotebookIdByNameAsync(_settings.DefaultNotebookName);
                    if (!string.IsNullOrEmpty(defaultNotebookId))
                    {
                        data["parent_id"] = defaultNotebookId;
                    }
                }

                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(GetApiUrl("notes"), content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<JoplinNote>(responseContent);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create note: {ex.Message}");
            }
        }

        public async Task<JoplinNote?> GetNoteByTitleAsync(string title)
        {
            try
            {
                var url = GetApiUrl("notes?fields=id,title,body&limit=100");
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JoplinResponse<JoplinNote>>(content);

                if (result?.Items == null) return null;

                var titleLower = title.Trim().ToLowerInvariant();
                var note = result.Items.FirstOrDefault(n =>
                    n.Title?.Trim().ToLowerInvariant() == titleLower);

                if (note != null) return note;

                // Check more pages if needed
                int page = 2;
                while (result.HasMore && page <= 10)
                {
                    url = GetApiUrl($"notes?fields=id,title,body&limit=100&page={page}");
                    response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    content = await response.Content.ReadAsStringAsync();
                    result = JsonSerializer.Deserialize<JoplinResponse<JoplinNote>>(content);

                    if (result?.Items == null) break;

                    note = result.Items.FirstOrDefault(n =>
                        n.Title?.Trim().ToLowerInvariant() == titleLower);

                    if (note != null) return note;

                    page++;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<JoplinNote?> UpdateNoteAsync(string noteId, string? title = null, string? body = null)
        {
            try
            {
                var data = new Dictionary<string, string>();

                if (!string.IsNullOrEmpty(title))
                {
                    data["title"] = title;
                }

                if (body != null) // Allow empty string
                {
                    data["body"] = body;
                }

                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(GetApiUrl($"notes/{noteId}"), content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<JoplinNote>(responseContent);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update note: {ex.Message}");
            }
        }

        public async Task<List<JoplinNotebook>> GetNotebooksAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(GetApiUrl("folders?fields=id,title"));
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JoplinResponse<JoplinNotebook>>(content);

                return result?.Items ?? new List<JoplinNotebook>();
            }
            catch
            {
                return new List<JoplinNotebook>();
            }
        }

        public async Task<string?> GetNotebookIdByNameAsync(string notebookName)
        {
            if (string.IsNullOrEmpty(notebookName))
                return null;

            try
            {
                var notebooks = await GetNotebooksAsync();
                var nameLower = notebookName.Trim().ToLowerInvariant();

                var notebook = notebooks.FirstOrDefault(nb =>
                    nb.Title?.Trim().ToLowerInvariant() == nameLower);

                return notebook?.Id;
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class JoplinNote
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("parent_id")]
        public string? ParentId { get; set; }
    }

    public class JoplinNotebook
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }

    public class JoplinResponse<T>
    {
        [JsonPropertyName("items")]
        public List<T>? Items { get; set; }

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }
    }
}
