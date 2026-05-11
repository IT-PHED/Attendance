using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace AttendanceManager.Services
{

    public class DbService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<DbService> _logger;

        public DbService(IConfiguration config, ILogger<DbService> logger)
        {
            _config = config;
            _logger = logger;
        }

        private IDbConnection CreateConnection()
        {
            var connectionString = _config.GetConnectionString("Database");
            _logger.LogInformation("Creating database connection for 'Database' connection string. Present={HasConnectionString}", !string.IsNullOrEmpty(connectionString));

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Database connection string 'Database' is not configured. Check appsettings and environment variables.");
            }

            return new SqlConnection(connectionString);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null)
        {
            _logger.LogDebug("DbService.QueryAsync SQL={Sql} Params={Params}", sql, param);
            try
            {
                using var conn = CreateConnection();
                return await conn.QueryAsync<T>(sql, param);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DbService.QueryAsync failed for SQL={Sql}", sql);
                throw;
            }
        }

        public async Task<T> QuerySingleAsync<T>(string sql, object param = null)
        {
            _logger.LogDebug("DbService.QuerySingleAsync SQL={Sql} Params={Params}", sql, param);
            try
            {
                using var conn = CreateConnection();
                return await conn.QueryFirstOrDefaultAsync<T>(sql, param);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DbService.QuerySingleAsync failed for SQL={Sql}", sql);
                throw;
            }
        }

        public async Task<int> ExecuteAsync(string sql, object param = null)
        {
            _logger.LogDebug("DbService.ExecuteAsync SQL={Sql} Params={Params}", sql, param);
            try
            {
                using var conn = CreateConnection();
                return await conn.ExecuteAsync(sql, param);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DbService.ExecuteAsync failed for SQL={Sql}", sql);
                throw;
            }
        }
    }
}

