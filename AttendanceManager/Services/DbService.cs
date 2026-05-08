using System.Data;
using Dapper;
using MySqlConnector;

namespace AttendanceManager.Services
{

    public class DbService
    {
        private readonly IConfiguration _config;

        public DbService(IConfiguration config)
        {
            _config = config;
        }

        private IDbConnection CreateConnection()
            => new MySqlConnection(_config.GetConnectionString("DefaultConnection"));

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null)
        {
            using var conn = CreateConnection();
            return await conn.QueryAsync<T>(sql, param);
        }

        public async Task<T> QuerySingleAsync<T>(string sql, object param = null)
        {
            using var conn = CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<T>(sql, param);
        }

        public async Task<int> ExecuteAsync(string sql, object param = null)
        {
            using var conn = CreateConnection();
            return await conn.ExecuteAsync(sql, param);
        }
    }
}

