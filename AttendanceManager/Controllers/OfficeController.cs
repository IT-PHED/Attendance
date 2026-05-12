using AttendanceManager.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace AttendanceManager.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class OfficeController : ControllerBase
    {
        private readonly DbService _db;

        public OfficeController(DbService db)
        {
            _db = db;
        }

        [HttpGet("/api/v1/office")]
        public async Task<JsonResult> GetOffices()
        {
            var sql = @"
                SELECT 
                    name AS OfficeDescription,
                    name AS Name,
                    address AS Address,
                    longitude AS Longitude,
                    latitude AS Latitude
                FROM offices;";

            var offices = await _db.QueryAsync<OfficeResponse>(
                sql);

            return CorsJson(offices);
        }

        [HttpGet("/api/v1/getStaff")]
        public async Task<JsonResult> GetStaff()
        {
            var sql = @"
                SELECT EMAIL, NAME, STAFF_ID AS StaffNumber
                FROM TBL_PROFILE
                WHERE IS_ACTIVE = 1;";

            var users = await _db.QueryAsync<StaffResponse>(
                sql);

            return CorsJson(users);
        }

        // GET api/v1/getAStaff?staff_number=12345
        [HttpGet("/api/v1/getAStaff")]
        public async Task<JsonResult> GetAStaff([FromQuery] string staff_number)
        {
            var sql = @"
                SELECT TOP (1) EMAIL, NAME, STAFF_ID AS StaffNumber
                FROM TBL_PROFILE
                WHERE STAFF_ID = @StaffNumber
                AND IS_ACTIVE = 1;";

            var user = await _db.QuerySingleAsync<StaffResponse>(
                sql,
                new { StaffNumber = staff_number });

            return CorsJson(user);
        }

        // GET api/v1/my_attendance?staff_id=17316&history=true
        [HttpGet("/api/v1/my_attendance")]
        public async Task<JsonResult> MyAttendance([FromQuery] string staff_id, [FromQuery] int history = 0)
        {
            var today = DateTime.Today;
            var from = history == 1 ? today.AddDays(-31) : today;
            var to = today;

            var sql = @"
                SELECT
                    staff AS Staff,
                    FORMAT(time, 'yyyy-MM-dd HH:mm:ss') AS Time,
                    FORMAT(timeout, 'yyyy-MM-dd HH:mm:ss') AS Timeout,
                    COALESCE(hours_worked, '0') AS HoursWorked,
                    image AS Image,
                    latitude AS Latitude,
                    longitude AS Longitude,
                    id AS Id,
                    COALESCE(office_name, '') AS OfficeName,
                    device_id AS DeviceId,
                    COALESCE(name, '') AS Name,
                    version AS Version
                FROM attendance
                WHERE staff = @StaffId
                    AND CAST(time AS DATE) BETWEEN @FromDate AND @ToDate
                ORDER BY time DESC;";

            var myAttendance = await _db.QueryAsync<MyAttendanceResponse>(
                sql,
                new
                {
                    StaffId = staff_id,
                    FromDate = from.ToString("yyyy-MM-dd"),
                    ToDate = to.ToString("yyyy-MM-dd")
                });

            return CorsJson(myAttendance);
        }

        // GET api/v1/get_staff_attendance?from=2026-01-01&to=2026-01-31&staffId=123
        [HttpGet("/api/v1/get_staff_attendance")]
        public async Task<JsonResult> GetStaffAttendanceGet([FromQuery] StaffAttendanceRequest request)
        {
            return await GetStaffAttendanceInternal(request);
        }

        // POST api/v1/get_staff_attendance
        [HttpPost("/api/v1/get_staff_attendance")]
        public async Task<JsonResult> GetStaffAttendancePost([FromBody] StaffAttendanceRequest request)
        {
            return await GetStaffAttendanceInternal(request);
        }

        private async Task<JsonResult> GetStaffAttendanceInternal(StaffAttendanceRequest request)
        {
            if (!ModelState.IsValid)
            {
                return CorsJson(new
                {
                    success = false,
                    message = "Validation failed",
                    errors = ModelState
                }, StatusCodes.Status422UnprocessableEntity);
            }

            var results = await _db.QueryAsync<object>(
                "EXEC GetStaffAttendance @From, @To, @StaffId;",
                new
                {
                    From = request.From,
                    To = request.To,
                    StaffId = request.StaffId
                });

            return CorsJson(new
            {
                success = true,
                data = results,
                report_date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                parameters = new
                {
                    from = request.From,
                    to = request.To,
                    staffId = request.StaffId
                }
            });
        }

        private JsonResult CorsJson(object? data, int statusCode = StatusCodes.Status200OK)
        {
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
            Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";

            return new JsonResult(data) { StatusCode = statusCode };
        }

        private sealed class OfficeResponse
        {
            [JsonPropertyName("OfficeDescription")]
            public string OfficeDescription { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public decimal Longitude { get; set; } = 0m;
            public decimal Latitude { get; set; } = 0m;
        }

        private sealed class StaffResponse
        {
            public string? Email { get; set; }
            public string? Name { get; set; }

            [JsonPropertyName("staff_number")]
            public string? StaffNumber { get; set; }
        }

        private sealed class MyAttendanceResponse
        {
            public string Staff { get; set; } = string.Empty;
            public string Time { get; set; } = string.Empty;
            public string? Timeout { get; set; }
            public string HoursWorked { get; set; } = "0";
            public string? Image { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public int Id { get; set; }
            [JsonPropertyName("office_name")]
            public string OfficeName { get; set; } = string.Empty;
            public string? DeviceId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Version { get; set; }
        }

        public sealed class StaffAttendanceRequest
        {
            [Required]
            [JsonPropertyName("from")]
            public string From { get; set; } = string.Empty;

            [Required]
            [JsonPropertyName("to")]
            public string To { get; set; } = string.Empty;

            [Required]
            [JsonPropertyName("staffId")]
            public string StaffId { get; set; } = string.Empty;
        }

    }
}
