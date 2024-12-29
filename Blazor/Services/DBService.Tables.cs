namespace Blazor.Services
{
    public partial class DBService
    {
        public List<string> GetAllTables()
        {
            List<string> tables = new List<string>();

            try
            {
                using var connection = new Npgsql.NpgsqlConnection(_connectionString);
                connection.Open();

                string sql = @"
                    SELECT table_name 
                    FROM information_schema.tables
                    WHERE table_schema = 'public'";

                using var cmd = new Npgsql.NpgsqlCommand(sql, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fejl ved hentning af tabeller: {ex.Message}");
            }

            return tables;
        }

        public class ColumnInfo
        {
            public string ColumnName { get; set; } = string.Empty;
            public string DataType { get; set; } = string.Empty;
            public bool IsPrimaryKey { get; set; }
            public bool IsForeignKey { get; set; }
            public string ReferencedTable { get; set; } = string.Empty;
            public string ReferencedColumn { get; set; } = string.Empty;
        }

        public Dictionary<string, List<ColumnInfo>> GetDatabaseSchema()
        {
            var schema = new Dictionary<string, List<ColumnInfo>>();

            try
            {
                using var connection = new Npgsql.NpgsqlConnection(_connectionString);
                connection.Open();

                string sql = @"
                    SELECT 
                        t.table_name,
                        c.column_name,
                        c.data_type,
                        CASE 
                            WHEN pk.constraint_type = 'PRIMARY KEY' THEN true 
                            ELSE false 
                        END as is_primary_key,
                        CASE 
                            WHEN fk.constraint_type = 'FOREIGN KEY' THEN true 
                            ELSE false 
                        END as is_foreign_key,
                        fk_info.foreign_table_name as referenced_table,
                        fk_info.foreign_column_name as referenced_column
                    FROM information_schema.tables t
                    JOIN information_schema.columns c 
                        ON t.table_name = c.table_name
                    LEFT JOIN (
                        SELECT ku.table_name, ku.column_name, tc.constraint_type
                        FROM information_schema.table_constraints tc
                        JOIN information_schema.key_column_usage ku
                            ON tc.constraint_name = ku.constraint_name
                        WHERE tc.constraint_type = 'PRIMARY KEY'
                    ) pk 
                        ON t.table_name = pk.table_name 
                        AND c.column_name = pk.column_name
                    LEFT JOIN (
                        SELECT ku.table_name, ku.column_name, tc.constraint_type
                        FROM information_schema.table_constraints tc
                        JOIN information_schema.key_column_usage ku
                            ON tc.constraint_name = ku.constraint_name
                        WHERE tc.constraint_type = 'FOREIGN KEY'
                    ) fk 
                        ON t.table_name = fk.table_name 
                        AND c.column_name = fk.column_name
                    LEFT JOIN (
                        SELECT 
                            kcu.table_name,
                            kcu.column_name,
                            ccu.table_name AS foreign_table_name,
                            ccu.column_name AS foreign_column_name
                        FROM information_schema.table_constraints AS tc 
                        JOIN information_schema.key_column_usage AS kcu
                            ON tc.constraint_name = kcu.constraint_name
                        JOIN information_schema.constraint_column_usage AS ccu
                            ON ccu.constraint_name = tc.constraint_name
                        WHERE tc.constraint_type = 'FOREIGN KEY'
                    ) fk_info
                        ON t.table_name = fk_info.table_name 
                        AND c.column_name = fk_info.column_name
                    WHERE t.table_schema = 'public'
                    ORDER BY t.table_name, c.ordinal_position;";

                using var cmd = new Npgsql.NpgsqlCommand(sql, connection);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var tableName = reader.GetString(0);
                    if (!schema.ContainsKey(tableName))
                    {
                        schema[tableName] = new List<ColumnInfo>();
                    }

                    schema[tableName].Add(new ColumnInfo
                    {
                        ColumnName = reader.GetString(1),
                        DataType = reader.GetString(2),
                        IsPrimaryKey = reader.GetBoolean(3),
                        IsForeignKey = reader.GetBoolean(4),
                        ReferencedTable = !reader.IsDBNull(5) ? reader.GetString(5) : string.Empty,
                        ReferencedColumn = !reader.IsDBNull(6) ? reader.GetString(6) : string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fejl ved hentning af database schema: {ex.Message}");
            }

            return schema;
        }

        public string GenerateMermaidERDiagram()
        {
            var schema = GetDatabaseSchema();
            var diagram = new System.Text.StringBuilder();
            diagram.AppendLine("erDiagram");

            // Tilføj alle tabeller og deres kolonner
            foreach (var table in schema)
            {
                diagram.AppendLine($"    {table.Key.ToUpper()} {{");
                foreach (var column in table.Value)
                {
                    var dataType = column.DataType
                        .Replace("character varying", "string")
                        .Replace("timestamp with time zone", "timestamp")
                        .Replace("numeric", "number")
                        .Replace("integer", "int")
                        .Replace("boolean", "bool")
                        .Replace("text", "string");
                    diagram.AppendLine($"        {dataType} {column.ColumnName}");
                }
                diagram.AppendLine("    }");
            }

            // Tilføj alle relationer
            foreach (var table in schema)
            {
                foreach (var column in table.Value.Where(c => c.IsForeignKey))
                {
                    diagram.AppendLine($"    {table.Key.ToUpper()} }}|--|| {column.ReferencedTable.ToUpper()} : references");
                }
            }

            return diagram.ToString();
        }
    }
}