using Microsoft.Data.Sqlite;
using EvvaAgent.Domain;
using EvvaAgent.Domain.Repositories;

namespace EvvaAgent.Infrastructure.Data
{
    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly string _connectionString;

        public ConfigurationRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=evva-agent.db";
        }

        public async Task<Configuration?> GetAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT `first-run`, `admin-server-url`, token, `nginx-path`, `nginx-config-path` FROM configurations LIMIT 1";

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Configuration
                {
                    FirstRun = reader.IsDBNull(0) ? null : reader.GetBoolean(0),
                    AdminServerUrl = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Token = reader.IsDBNull(2) ? null : reader.GetString(2),
                    NginxPath = reader.IsDBNull(3) ? null : reader.GetString(3),
                    NginxConfigPath = reader.IsDBNull(4) ? null : reader.GetString(4)
                };
            }

            return null;
        }

        public async Task SaveAsync(Configuration configuration)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO configurations (`first-run`, `admin-server-url`, token, `nginx-path`, `nginx-config-path`) 
                VALUES (@firstRun, @adminServerUrl, @token, @nginxPath, @nginxConfigPath)";
            
            command.Parameters.AddWithValue("@firstRun", configuration.FirstRun ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@adminServerUrl", configuration.AdminServerUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@token", configuration.Token ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@nginxPath", configuration.NginxPath ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@nginxConfigPath", configuration.NginxConfigPath ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }
    }
}