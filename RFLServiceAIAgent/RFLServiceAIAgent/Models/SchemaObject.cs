namespace RFLServiceAIAgent.Models
{
    public class SchemaObject
    {
        public string Header { get; set; } = "";

        public List<string> Columns { get; set; } = [];

        public int Score { get; set; }
    }
}
