using RFLServiceAIAgent.Tools.InterFace;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace RFLServiceAIAgent.Tools
{
    public class SchemaTool: ISchemaTool
    {
        private readonly IConfiguration _configuration;

        public SchemaTool(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> GetSchemaAsync()
        {
            var connectionString =
                _configuration.GetConnectionString("DefaultConnection");

            await using var connection =
                new SqlConnection(connectionString);

            await connection.OpenAsync();

            var result = new StringBuilder();

            // ----------------------------------------------------
            // 1. Get tables/views + columns + primary key
            // ----------------------------------------------------

            var columnSql = """
        SELECT
            c.TABLE_SCHEMA,
            c.TABLE_NAME,
            c.COLUMN_NAME,
            c.DATA_TYPE,

            CASE
                WHEN t.TABLE_TYPE = 'VIEW'
                    THEN 'VIEW'
                ELSE 'TABLE'
            END AS OBJECT_TYPE,

            CASE
                WHEN pk.COLUMN_NAME IS NOT NULL
                    THEN 1
                ELSE 0
            END AS IS_PRIMARY_KEY

        FROM INFORMATION_SCHEMA.COLUMNS c

        INNER JOIN INFORMATION_SCHEMA.TABLES t
            ON c.TABLE_SCHEMA = t.TABLE_SCHEMA
            AND c.TABLE_NAME = t.TABLE_NAME

        LEFT JOIN
        (
            SELECT
                kcu.TABLE_SCHEMA,
                kcu.TABLE_NAME,
                kcu.COLUMN_NAME

            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc

            INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                AND tc.TABLE_SCHEMA = kcu.TABLE_SCHEMA
                AND tc.TABLE_NAME = kcu.TABLE_NAME

            WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
        ) pk

            ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA
            AND c.TABLE_NAME = pk.TABLE_NAME
            AND c.COLUMN_NAME = pk.COLUMN_NAME

        ORDER BY
            c.TABLE_SCHEMA,
            c.TABLE_NAME,
            c.ORDINAL_POSITION;
        """;

            var columns = new List<(
                string Schema,
                string Table,
                string Column,
                string DataType,
                string ObjectType,
                bool IsPrimaryKey
            )>();

            await using (var command =
                new SqlCommand(columnSql, connection))
            {
                await using var reader =
                    await command.ExecuteReaderAsync();

                string? currentObject = null;

                while (await reader.ReadAsync())
                {
                    var schema =
                        reader["TABLE_SCHEMA"].ToString()!;

                    var table =
                        reader["TABLE_NAME"].ToString()!;

                    var column =
                        reader["COLUMN_NAME"].ToString()!;

                    var dataType =
                        reader["DATA_TYPE"].ToString()!;

                    var objectType =
                        reader["OBJECT_TYPE"].ToString()!;

                    var isPrimaryKey =
                        Convert.ToInt32(
                            reader["IS_PRIMARY_KEY"]) == 1;

                    columns.Add(
                        (
                            schema,
                            table,
                            column,
                            dataType,
                            objectType,
                            isPrimaryKey
                        ));

                    var objectName =
                        $"{schema}.{table}";

                    if (currentObject != objectName)
                    {
                        currentObject = objectName;

                        result.AppendLine();

                        result.AppendLine(
                            $"OBJECT: {objectType} {objectName}");
                    }

                    result.Append(
                        $"  COLUMN: {column} ({dataType})");

                    if (isPrimaryKey)
                    {
                        result.Append(" [PK]");
                    }

                    result.AppendLine();
                }
            }

            // ----------------------------------------------------
            // 2. Get actual SQL Server foreign key relationships
            // ----------------------------------------------------

            var foreignKeySql = """
        SELECT
            sch1.name AS ParentSchema,
            t1.name AS ParentTable,
            c1.name AS ParentColumn,

            sch2.name AS ReferencedSchema,
            t2.name AS ReferencedTable,
            c2.name AS ReferencedColumn

        FROM sys.foreign_key_columns fkc

        INNER JOIN sys.tables t1
            ON fkc.parent_object_id = t1.object_id

        INNER JOIN sys.schemas sch1
            ON t1.schema_id = sch1.schema_id

        INNER JOIN sys.columns c1
            ON fkc.parent_object_id = c1.object_id
            AND fkc.parent_column_id = c1.column_id

        INNER JOIN sys.tables t2
            ON fkc.referenced_object_id = t2.object_id

        INNER JOIN sys.schemas sch2
            ON t2.schema_id = sch2.schema_id

        INNER JOIN sys.columns c2
            ON fkc.referenced_object_id = c2.object_id
            AND fkc.referenced_column_id = c2.column_id

        ORDER BY
            sch1.name,
            t1.name,
            c1.name;
        """;

            result.AppendLine();
            result.AppendLine("RELATIONSHIPS:");

            await using (var command =
                new SqlCommand(foreignKeySql, connection))
            {
                await using var reader =
                    await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var parentSchema =
                        reader["ParentSchema"].ToString();

                    var parentTable =
                        reader["ParentTable"].ToString();

                    var parentColumn =
                        reader["ParentColumn"].ToString();

                    var referencedSchema =
                        reader["ReferencedSchema"].ToString();

                    var referencedTable =
                        reader["ReferencedTable"].ToString();

                    var referencedColumn =
                        reader["ReferencedColumn"].ToString();

                    result.AppendLine(
                        $"  {parentSchema}.{parentTable}.{parentColumn}" +
                        $" -> " +
                        $"{referencedSchema}.{referencedTable}.{referencedColumn}");
                }
            }

            return result.ToString();
        }
    }
}
