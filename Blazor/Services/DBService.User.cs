using Blazor.Components.Pages;
using Domain_Models;
using Npgsql;

namespace Blazor.Services
{
    public partial class DBService
    {
        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                Console.WriteLine("Getting all Users");
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                string sql = @"SELECT id, username, email, password_hash, created_at, updated_at, last_login, is_active, first_name, last_name, role_id
                                FROM ""User""";
                Console.WriteLine(sql);

                await using var command = new NpgsqlCommand(sql, connection);
                await using var reader = await command.ExecuteReaderAsync();

                List<User> users = new List<User>();
                while (await reader.ReadAsync())
                {
                    users.Add(MapSQLToUser(reader));
                }
                return users;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database fejl: {ex.Message}");
                throw;
            }
        }

        public async Task<User> GetUserByIdAsync(string id)
        {
            try
            {
                string sql = @"SELECT * FROM ""User"" 
                                WHERE id = @id";
                using var connection = new NpgsqlConnection(_connectionString);
                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@id", id);
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return null;

                return MapSQLToUser(reader);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task<User> PostUserAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (PasswordValidator(user.PasswordHash) != "Password is valid")
                throw new ArgumentException(PasswordValidator(user.PasswordHash));

            try
            {
                // Hash password med specifikke options
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                    user.PasswordHash,
                    workFactor: 11,
                    enhancedEntropy: false
                );

                Console.WriteLine($"Generated hash: {user.PasswordHash}");

                string sql = @"INSERT INTO ""User"" 
                (id, username, email, password_hash, created_at, updated_at, last_login, is_active, first_name, last_name, role_id) 
                VALUES (@id, @username, @email, @password_hash, @created_at, @updated_at, @last_login, @is_active, @first_name, @last_name, @role_id)";


                using var connection = new NpgsqlConnection(_connectionString);
                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("@username", user.Username ?? string.Empty);
                command.Parameters.AddWithValue("@email", user.Email ?? string.Empty);
                command.Parameters.AddWithValue("@password_hash", user.PasswordHash ?? string.Empty);
                command.Parameters.AddWithValue("@created_at", DateTime.UtcNow);
                command.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
                command.Parameters.AddWithValue("@last_login", DateTime.UtcNow);
                command.Parameters.AddWithValue("@is_active", false);
                command.Parameters.AddWithValue("@first_name", user.FirstName ?? string.Empty);
                command.Parameters.AddWithValue("@last_name", user.LastName ?? string.Empty);
                command.Parameters.AddWithValue("@role_id", 1);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user: {ex.Message}");
                throw;
            }
        }

        public async Task<User> PutUserAsync(User user)
        {
            try
            {
                string sql = @"UPDATE ""User"" 
                SET username = @username, 
                email = @email, 
                updated_at = @updated_at, 
                last_login = @last_login, 
                is_active = @is_active, 
                first_name = @first_name, 
                last_name = @last_name,
                role_id = @role_id
                WHERE id = @id";

                using var connection = new NpgsqlConnection(_connectionString);
                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@id", user.Id);
                command.Parameters.AddWithValue("@username", user.Username);
                command.Parameters.AddWithValue("@email", user.Email);
                command.Parameters.AddWithValue("@updated_at", user.UpdatedAt);
                command.Parameters.AddWithValue("@last_login", user.LastLogin ?? DateTime.UtcNow);
                command.Parameters.AddWithValue("@is_active", user.IsActive);
                command.Parameters.AddWithValue("@first_name", user.FirstName ?? string.Empty);
                command.Parameters.AddWithValue("@last_name", user.LastName ?? string.Empty);
                command.Parameters.AddWithValue("@role_id", user.RoleId);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task<string> DeleteUserAsync(string id)
        {
            try
            {
                string sql = @"DELETE FROM ""User"" 
                                WHERE id = @id";
                using var connection = new NpgsqlConnection(_connectionString);
                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@id", id);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                return $"User {id} deleted";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public User MapSQLToUser(NpgsqlDataReader reader)
        {
            try
            {
                return new User
                {
                    Id = SafeGetString(reader, "id"),
                    Username = SafeGetString(reader, "username") ?? "Dummy",
                    Email = SafeGetString(reader, "email") ?? "Dummy@dummy.com",
                    PasswordHash = SafeGetString(reader, "password_hash") ?? "Dummy",
                    CreatedAt = SafeGetDateTime(reader, "created_at") ?? DateTime.MinValue,
                    UpdatedAt = SafeGetDateTime(reader, "updated_at") ?? DateTime.MinValue,
                    LastLogin = SafeGetDateTime(reader, "last_login"),
                    IsActive = SafeGetBoolean(reader, "is_active") ?? false,
                    FirstName = SafeGetString(reader, "first_name"),
                    LastName = SafeGetString(reader, "last_name"),
                    RoleId = SafeGetInt32(reader, "role_id") ?? 1
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error mapping user: {ex.Message}");
                throw;
            }
        }



        public async Task<User?> AuthenticateUserAsync(string username, string password)
        {
            try
            {
                string sql = @"SELECT * FROM ""User"" WHERE username = @username";

                using var connection = new NpgsqlConnection(_connectionString);
                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@username", username);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    Console.WriteLine("User not found");
                    return null;
                }

                var user = MapSQLToUser(reader);

                Console.WriteLine($"Retrieved hash: {user.PasswordHash}");

                // Grundig validering af password hash
                if (string.IsNullOrEmpty(user.PasswordHash))
                {
                    Console.WriteLine("Password hash is null or empty");
                    return null;
                }


                // Verify password med ekstra sikkerhed
                bool isPasswordValid = false;
                try
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(
                        password,
                        user.PasswordHash,
                        false,
                        BCrypt.Net.HashType.SHA384
                    );

                    if (!isPasswordValid)
                    {
                        Console.WriteLine("Password verification failed");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Password verification error: {ex.Message}");
                    Console.WriteLine($"Input password length: {password?.Length ?? 0}");
                    Console.WriteLine($"Stored hash length: {user.PasswordHash?.Length ?? 0}");
                    return null;
                }

                // Update last login
                if (isPasswordValid)
                {
                    try
                    {
                        string updateSql = @"UPDATE ""User"" SET last_login = @lastLogin WHERE id = @id";
                        await reader.CloseAsync();
                        using var updateCommand = new NpgsqlCommand(updateSql, connection);
                        updateCommand.Parameters.AddWithValue("@lastLogin", DateTime.UtcNow);
                        updateCommand.Parameters.AddWithValue("@id", user.Id);
                        await updateCommand.ExecuteNonQueryAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating last login: {ex.Message}");
                    }
                }

                return isPasswordValid ? user : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication error: {ex.Message}");
                throw;
            }
        }

        public string PasswordValidator(string password)
        {
            if (password.Length < 6)
                return "Password must be at least 6 characters long";
            if (!password.Any(char.IsDigit))
                return "Password must contain at least one number";
            if (!password.Any(char.IsUpper))
                return "Password must contain at least one uppercase letter";
            if (!password.Any(char.IsLower))
                return "Password must contain at least one lowercase letter";
            return "Password is valid";
        }

        // Helper metoder til sikker læsning af data
        private string? SafeGetString(NpgsqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch
            {
                return null;
            }
        }

        private DateTime? SafeGetDateTime(NpgsqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
            }
            catch
            {
                return null;
            }
        }

        private bool? SafeGetBoolean(NpgsqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
            }
            catch
            {
                return null;
            }
        }

        private int? SafeGetInt32(NpgsqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
            }
            catch
            {
                return null;
            }
        }
    }
}