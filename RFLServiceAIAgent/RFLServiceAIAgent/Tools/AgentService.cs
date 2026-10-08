using RFLServiceAIAgent.Models;
using RFLServiceAIAgent.Service.InterFace;
using RFLServiceAIAgent.Tools.InterFace;
using System.Diagnostics;
using System.Text;

namespace RFLServiceAIAgent.Tools
{
    public class AgentService : IAgentService
    {
        private readonly ISchemaTool _schemaTool;
        private readonly IOllamaService _ollamaService;

        public AgentService(
            ISchemaTool schemaTool,
            IOllamaService ollamaService)
        {
            _schemaTool = schemaTool;
            _ollamaService = ollamaService;
        }
        public async Task<string> AskAsync(string question)
        {
            var fullSchema =
                await _schemaTool.GetSchemaAsync();

            var relevantSchema =
                SelectRelevantSchema(
                    question,
                    fullSchema);

                  var prompt = $"""
                    You are an AI assistant that generates SQL Server SELECT queries.

                    DATABASE STRUCTURE:
                    {relevantSchema}

                    USER QUESTION:
                    {question}

                    RULES:
                    1. Use only tables, views, and columns provided in the DATABASE STRUCTURE.
                    2. Do not invent tables.
                    3. Do not invent views.
                    4. Do not invent columns.
                    5. Do not invent relationships.
                    6. Use the actual column names exactly as provided.
                    7. Generate only a SQL Server SELECT query.
                    8. Do not use INSERT, UPDATE, DELETE, DROP, ALTER, TRUNCATE, EXEC, MERGE, or other database-modifying commands.
                    9. If the question cannot be answered using the provided database structure, return: CANNOT_ANSWER
                    10. Return only the SQL query. Do not include explanations, markdown, or ```sql```.

                    Generate the SQL query now.
                    """;

            var response =
                await _ollamaService.ChatAsync(prompt);

            return response;
        }
        private string SelectRelevantSchema(
           string question,
           string fullSchema)
        {
            var questionWords = question
                .ToLowerInvariant()
                .Split(
                    new[]
                    {
                ' ',
                ',',
                '.',
                '?',
                '!',
                ':',
                ';',
                '-',
                '_',
                '/',
                '\\'
                    },
                    StringSplitOptions.RemoveEmptyEntries)
                .Distinct()
                .ToList();

            var lines = fullSchema.Split(
                Environment.NewLine,
                StringSplitOptions.RemoveEmptyEntries);

            var objects = new List<SchemaObject>();

            SchemaObject? currentObject = null;

            foreach (var line in lines)
            {
                if (line.StartsWith(
                    "OBJECT:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    currentObject = new SchemaObject
                    {
                        Header = line
                    };

                    objects.Add(currentObject);

                    continue;
                }

                if (line.StartsWith(
                    "  COLUMN:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (currentObject != null)
                    {
                        currentObject.Columns.Add(line);
                    }

                    continue;
                }

                if (line.StartsWith(
                    "RELATIONSHIPS:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            foreach (var obj in objects)
            {
                var objectName = obj.Header
                    .Replace("OBJECT:", "")
                    .Trim()
                    .ToLowerInvariant();

                var objectNameParts = objectName
                    .Split(
                        '.',
                        StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in questionWords)
                {
                    if (word.Length < 2)
                        continue;

                    foreach (var part in objectNameParts)
                    {
                        if (part.Equals(
                            word,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            obj.Score += 10;
                        }
                        else if (part.Contains(
                            word,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            obj.Score += 5;
                        }
                    }

                    foreach (var column in obj.Columns)
                    {
                        var columnName = column
                            .Replace("COLUMN:", "")
                            .Trim()
                            .Split(' ')[0]
                            .ToLowerInvariant();

                        if (columnName.Equals(
                            word,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            obj.Score += 4;
                        }
                        else if (columnName.Contains(
                            word,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            obj.Score += 2;
                        }
                    }
                }
            }

            var relevantObjects = objects
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(10)
                .ToList();

            var result = new StringBuilder();

            foreach (var obj in relevantObjects)
            {
                result.AppendLine(obj.Header);

                foreach (var column in obj.Columns)
                {
                    result.AppendLine(column);
                }

                result.AppendLine();
            }

            return result.ToString();
        }
    }
}

