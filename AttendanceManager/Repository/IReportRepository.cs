namespace AttendanceManager.Repository
{
    public interface IReportRepository
    {
        Task<List<dynamic>> GetPunctualStaffReportAsync(string from, string to);
        Task<List<dynamic>> GetLateStaffReportAsync(string from, string to);
        Task<List<dynamic>> GetNightStaffReportAsync(string from, string to);
        Task<List<dynamic>> GetLeaveStaffReportAsync(string from, string to);
        Task<List<dynamic>> GetVisitStaffReportAsync(string from, string to);
        Task<List<dynamic>> GetAttendanceStatsAsync(string from, string to);
        Task<List<dynamic>> GetAbsentStaffReportAsync(string date);
    }
}
