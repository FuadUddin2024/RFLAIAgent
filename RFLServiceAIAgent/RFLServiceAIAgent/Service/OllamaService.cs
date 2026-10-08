using RFLServiceAIAgent.Models;
using RFLServiceAIAgent.Service.InterFace;
using System.Diagnostics;
using static RFLServiceAIAgent.Models.OllamaModel;

namespace RFLServiceAIAgent.Service
{
    public class OllamaService: IOllamaService
    {
        private readonly HttpClient _httpClient;

        public OllamaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<string> ChatAsync(string prompt)
        {
            var request = new OllamaChatRequest
            {
                Model = "qwen3:4b",
                Stream = false,

                Messages =
                [
                    new OllamaMessage
                {
                    Role = "user",
                    Content = prompt
                }
                ],

                Options = new OllamaOptions
                {
                    NumPredict = 50,
                    Temperature = 0.1
                },

                KeepAlive = "30m"
            };

            var response = await _httpClient.PostAsJsonAsync(
                "api/chat",
                request);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<OllamaChatResponse>();

            return result?.Message?.Content ?? string.Empty;
        }
    }
}
