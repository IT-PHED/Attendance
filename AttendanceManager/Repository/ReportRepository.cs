using AttendanceManager.Services;

namespace AttendanceManager.Repository
{
    public class ReportRepository : IReportRepository
    {
        private readonly DbService _db;

        public ReportRepository(DbService db)
        {
            _db = db;
        }

        public Task<List<dynamic>> GetPunctualStaffReportAsync(string from, string to) =>
            ExecuteTwoDateProcAsync("PunctualStaffReport", from, to);

        public Task<List<dynamic>> GetLateStaffReportAsync(string from, string to) =>
            ExecuteTwoDateProcAsync("LateStaffReport", from, to);

        public Task<List<dynamic>> GetNightStaffReportAsync(string from, string to) =>
            ExecuteTwoDateProcAsync("NightStaffReport", from, to);

        public Task<List<dynamic>> GetLeaveStaffReportAsync(string from, string to) =>
            ExecuteTwoDateProcAsync("LeaveStaffReport", from, to);

        public Task<List<dynamic>> GetVisitStaffReportAsync(string from, string to) =>
            ExecuteTwoDateProcAsync("VisitStaffReport", from, to);

        public Task<List<dynamic>> GetAttendanceStatsAsync(string from, string to) =>
            ExecuteTwoDateProcAsync("GetStaffAttendanceStats", from, to);

        public async Task<List<dynamic>> GetAbsentStaffReportAsync(string date)
        {
            const string sql = "EXEC AbsentStaffReport @Date;";
            var results = await _db.QueryAsync<dynamic>(sql, new { Date = date });
            return results.ToList();
        }

        private async Task<List<dynamic>> ExecuteTwoDateProcAsync(string procedureName, string from, string to)
        {
            var sql = $"EXEC {procedureName} @From, @To;";
            var results = await _db.QueryAsync<dynamic>(sql, new { From = from, To = to });
            return results.ToList();
        }
    }
}