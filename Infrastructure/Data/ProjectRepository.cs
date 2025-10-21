using Microsoft.Data.Sqlite;
using EvvaAgent.Domain;
using EvvaAgent.Domain.Repositories;

namespace EvvaAgent.Infrastructure.Data
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly string _connectionString;

        public ProjectRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=evva-agent.db";
        }

        public async Task<IEnumerable<Project>> GetAllAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT id, name, status, url, uuid, path, `auto-sync`, branch, service FROM projects";

            var projects = new List<Project>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                projects.Add(new Project
                {
                    Id = reader.IsDBNull(0) ? null : reader.GetInt32(0),
                    Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Status = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Url = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Uuid = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Path = reader.IsDBNull(5) ? null : reader.GetString(5),
                    AutoSync = reader.IsDBNull(6) ? false : reader.GetInt32(6) == 1,
                    Branch = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Service = reader.IsDBNull(8) ? null : reader.GetString(8)
                });
            }

            return projects;
        }

        public async Task<Project?> GetByIdAsync(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT id, name, status, url, uuid, path, `auto-sync`, branch, service FROM projects WHERE id = @id";
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Project
                {
                    Id = reader.IsDBNull(0) ? null : reader.GetInt32(0),
                    Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Status = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Url = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Uuid = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Path = reader.IsDBNull(5) ? null : reader.GetString(5),
                    AutoSync = reader.IsDBNull(6) ? false : reader.GetInt32(6) == 1,
                    Branch = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Service = reader.IsDBNull(8) ? null : reader.GetString(8)
                };
            }

            return null;
        }

        public async Task<Project?> GetByNameAsync(string name)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT id, name, status, url, uuid, path, `auto-sync`, branch, service FROM projects WHERE name = @name";
            command.Parameters.AddWithValue("@name", name);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Project
                {
                    Id = reader.IsDBNull(0) ? null : reader.GetInt32(0),
                    Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Status = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Url = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Uuid = reader.IsDBNull(4) ? null : reader.GetString(4),
                    Path = reader.IsDBNull(5) ? null : reader.GetString(5),
                    AutoSync = reader.IsDBNull(6) ? false : reader.GetInt32(6) == 1,
                    Branch = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Service = reader.IsDBNull(8) ? null : reader.GetString(8)
                };
            }

            return null;
        }

        public async Task SaveAsync(Project project)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO projects (id, name, status, url, uuid, path, `auto-sync`, branch, service) 
                VALUES (@id, @name, @status, @url, @uuid, @path, @autoSync, @branch, @service)";
            
            command.Parameters.AddWithValue("@id", project.Id ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@name", project.Name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@status", project.Status ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@url", project.Url ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@uuid", project.Uuid ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@path", project.Path ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@autoSync", project.AutoSync ? 1 : 0);
            command.Parameters.AddWithValue("@branch", project.Branch ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@service", project.Service ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM projects WHERE id = @id";
            command.Parameters.AddWithValue("@id", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteByNameAsync(string name)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM projects WHERE name = @name";
            command.Parameters.AddWithValue("@name", name);

            await command.ExecuteNonQueryAsync();
        }
    }
}