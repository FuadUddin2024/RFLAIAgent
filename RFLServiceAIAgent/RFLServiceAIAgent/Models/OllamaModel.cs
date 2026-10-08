using System.Text.Json.Serialization;

namespace RFLServiceAIAgent.Models
{
    public class OllamaModel
    {
    }
    public class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OllamaMessage> Messages { get; set; } = [];

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
        [JsonPropertyName("options")]
        public OllamaOptions? Options { get; set; }
        [JsonPropertyName("keep_alive")]
        public string? KeepAlive { get; set; }
    }
    public class OllamaOptions
    {
        [JsonPropertyName("num_predict")]
        public int NumPredict { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
    }
    public class OllamaMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }
    }
}
