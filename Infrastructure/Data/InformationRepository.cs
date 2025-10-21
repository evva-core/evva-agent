using Microsoft.Data.Sqlite;
using EvvaAgent.Domain;
using EvvaAgent.Domain.Repositories;

namespace EvvaAgent.Infrastructure.Data
{
    public class InformationRepository : IInformationRepository
    {
        private readonly string _connectionString;

        public InformationRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=evva-agent.db";
        }

        public async Task<Information?> GetAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT `host-name`, `ip-address`, os, arch, `is-docker-host`, uuid FROM information LIMIT 1";

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Information
                {
                    HostName = reader.GetString(0),
                    IpAddress = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Os = reader.GetString(2),
                    Arch = reader.GetString(3),
                    IsDockerHost = reader.GetInt32(4) == 1,
                    Uuid = reader.GetString(5)
                };
            }

            return null;
        }

        public async Task SaveAsync(Information information)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO information (`host-name`, `ip-address`, os, arch, `is-docker-host`, uuid) 
                VALUES (@hostName, @ipAddress, @os, @arch, @isDockerHost, @uuid)";
            
            command.Parameters.AddWithValue("@hostName", information.HostName);
            command.Parameters.AddWithValue("@ipAddress", information.IpAddress ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@os", information.Os);
            command.Parameters.AddWithValue("@arch", information.Arch);
            command.Parameters.AddWithValue("@isDockerHost", information.IsDockerHost ? 1 : 0);
            command.Parameters.AddWithValue("@uuid", information.Uuid);

            await command.ExecuteNonQueryAsync();
        }
    }
}