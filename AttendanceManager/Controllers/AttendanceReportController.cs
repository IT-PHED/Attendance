using AttendanceManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceManager.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class ReportController : ControllerBase
    {
        private readonly ReportService _reportService;

        public ReportController(ReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("attendancereportpunctual")]
        public Task<IActionResult> GetPunctual([FromQuery] string from, [FromQuery] string to) =>
            HandleDateRangeReport(from, to, "punctual", _reportService.GetPunctualAsync);

        [HttpGet("attendancereportlate")]
        public Task<IActionResult> GetLate([FromQuery] string from, [FromQuery] string to) =>
            HandleDateRangeReport(from, to, "late", _reportService.GetLateAsync);

        [HttpGet("attendancereportnight")]
        public Task<IActionResult> GetNight([FromQuery] string from, [FromQuery] string to) =>
            HandleDateRangeReport(from, to, "night", _reportService.GetNightAsync);

        [HttpGet("attendancereportleave")]
        public Task<IActionResult> GetLeave([FromQuery] string from, [FromQuery] string to) =>
            HandleDateRangeReport(from, to, "leave", _reportService.GetLeaveAsync);

        [HttpGet("attendancereportvisit")]
        public Task<IActionResult> GetVisit([FromQuery] string from, [FromQuery] string to) =>
            HandleDateRangeReport(from, to, "visit", _reportService.GetVisitAsync);

        [HttpGet("attendancereportstatistics")]
        public Task<IActionResult> GetStatistics([FromQuery] string from, [FromQuery] string to) =>
            HandleDateRangeReport(from, to, "stats", _reportService.GetStatisticsAsync);

        [HttpGet("attendancereportabsent")]
        public async Task<IActionResult> GetAbsent([FromQuery] string date)
        {
            if (!ReportService.IsIsoDate(date))
            {
                return UnprocessableEntity(new
                {
                    success = false,
                    message = "Validation failed",
                    errors = new { date = new[] { "The date field must use YYYY-MM-DD format." } }
                });
            }

            var data = await _reportService.GetAbsentAsync(date);
            return Ok(BuildResponse(data, "absent"));
        }

        private async Task<IActionResult> HandleDateRangeReport(
            string from,
            string to,
            string type,
            Func<string, string, Task<List<dynamic>>> reportFn)
        {
            if (!ReportService.IsIsoDate(from) || !ReportService.IsIsoDate(to))
            {
                return UnprocessableEntity(new
                {
                    success = false,
                    message = "Validation failed",
                    errors = new
                    {
                        from = !ReportService.IsIsoDate(from) ? new[] { "The from field must use YYYY-MM-DD format." } : Array.Empty<string>(),
                        to = !ReportService.IsIsoDate(to) ? new[] { "The to field must use YYYY-MM-DD format." } : Array.Empty<string>()
                    }
                });
            }

            var data = await reportFn(from, to);
            return Ok(BuildResponse(data, type));
        }

        private static object BuildResponse(List<dynamic> data, string type) => new
        {
            success = true,
            data,
            report_date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            count = data.Count,
            type
        };
    }
}