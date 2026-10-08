namespace RFLServiceAIAgent.Service.InterFace
{
    public interface IOllamaService
    {
        Task<string> ChatAsync(string prompt);
    }
}
