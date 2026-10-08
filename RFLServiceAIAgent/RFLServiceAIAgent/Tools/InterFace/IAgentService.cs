namespace RFLServiceAIAgent.Tools.InterFace
{
    public interface IAgentService
    {
        Task<string> AskAsync(string question);
    }
}
