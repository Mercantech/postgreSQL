using Npgsql;

namespace Blazor.Services
{
    public partial class DBService
    {
        private readonly string _connectionString;
        public DBService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public bool TestConnection()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database forbindelsesfejl: {ex.Message}");
                return false;
            }
        }

        private async Task<string> ReadSqlFileAsync(string filePath)
        {
            try
            {
                return await File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fejl ved læsning af SQL-fil: {ex.Message}");
                throw;
            }
        }

        public async Task ExecuteSqlFileAsync(string filePath)
        {
            try
            {
                string sql = await ReadSqlFileAsync(filePath);

                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();

                using var cmd = new NpgsqlCommand(sql, connection);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fejl ved udførelse af SQL-fil: {ex.Message}");
                throw;
            }
        }


    }
}
