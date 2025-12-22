using System.Text.Json.Serialization;

namespace Flow.Launcher.Plugin.Joplin
{
    public class Settings
    {
        [JsonPropertyName("api_token")]
        public string ApiToken { get; set; } = string.Empty;

        [JsonPropertyName("api_port")]
        public string ApiPort { get; set; } = "41184";

        [JsonPropertyName("default_notebook_name")]
        public string DefaultNotebookName { get; set; } = string.Empty;

        public int GetPort()
        {
            if (int.TryParse(ApiPort, out int port))
            {
                return port;
            }
            return 41184; // Default port
        }
    }
}
