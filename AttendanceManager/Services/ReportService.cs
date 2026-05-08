using AttendanceManager.Repository;
using System.Globalization;

namespace AttendanceManager.Services
{
    public class ReportService
    {
        private readonly IReportRepository _reportRepository;

        public ReportService(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public Task<List<dynamic>> GetPunctualAsync(string from, string to) =>
            _reportRepository.GetPunctualStaffReportAsync(from, to);

        public Task<List<dynamic>> GetLateAsync(string from, string to) =>
            _reportRepository.GetLateStaffReportAsync(from, to);

        public Task<List<dynamic>> GetNightAsync(string from, string to) =>
            _reportRepository.GetNightStaffReportAsync(from, to);

        public Task<List<dynamic>> GetLeaveAsync(string from, string to) =>
            _reportRepository.GetLeaveStaffReportAsync(from, to);

        public Task<List<dynamic>> GetVisitAsync(string from, string to) =>
            _reportRepository.GetVisitStaffReportAsync(from, to);

        public Task<List<dynamic>> GetStatisticsAsync(string from, string to) =>
            _reportRepository.GetAttendanceStatsAsync(from, to);

        public Task<List<dynamic>> GetAbsentAsync(string date) =>
            _reportRepository.GetAbsentStaffReportAsync(date);

        public static bool IsIsoDate(string value) =>
            DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }
}
