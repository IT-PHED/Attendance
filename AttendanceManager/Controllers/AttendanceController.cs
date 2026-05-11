using AttendanceManager.Models;
using AttendanceManager.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace AttendanceManager.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AttendanceController : ControllerBase
    {
        private readonly AttendanceService _attendanceService;

        public AttendanceController(AttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        // POST api/v1/attendance/upload
        [HttpPost("upload")]
        public async Task<JsonResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return CorsJson(new { success = false, message = "File is required" }, StatusCodes.Status400BadRequest);
            }

            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".mp4", ".png", ".jpeg", ".jpg", ".txt", ".csv", ".mp3", ".hevc", ".mov"
            };
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
            {
                return CorsJson(new { success = false, message = "Invalid file extension" }, StatusCodes.Status400BadRequest);
            }

            var currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            var folder = Path.Combine("wwwroot", "public", "files", currentDate);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var originalFilename = Path.GetFileName(file.FileName);
            var filename = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{originalFilename}";
            var physicalPath = Path.Combine(folder, filename);
            var relativePath = $"public/files/{currentDate}/{filename}";

            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return CorsJson(new
            {
                success = true,
                message = "File successfully uploaded",
                path = relativePath
            });
        }

        // POST api/v1/attendance/store
        [HttpPost("store")]
        public async Task<JsonResult> Store([FromBody] InttendanceFormat request)
        {
            var requestedWithHeader = HttpContext.Request.Headers["X-Requested-With"].ToString();
            var hasXmlHttpHeader = requestedWithHeader.Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

            if (!hasXmlHttpHeader)
            {
                return CorsJson(new
                {
                    success = false,
                    message = "Invalid request: Missing required X-Requested-With header"
                }, StatusCodes.Status400BadRequest);
            }

            DateTime parsedTime;
            DateTime? parsedTimeout = null;

            try
            {
                parsedTime = DateTime.ParseExact(
                    request.Time,
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture
                );

                if (!string.IsNullOrEmpty(request.Timeout))
                {
                    parsedTimeout = DateTime.ParseExact(
                        request.Timeout,
                        "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture
                    );
                }
            }
            catch (FormatException)
            {
                return CorsJson(new
                {
                    success = false,
                    message = "Invalid date format. Expected yyyy-MM-dd HH:mm:ss"
                }, StatusCodes.Status400BadRequest);
            }

            var attendance = new Inttendance
            {
                Id = request.Id,
                Time = parsedTime,
                Timeout = parsedTimeout,
                lat = request.lat,
                @long = request.@long,
                Image = request.Image,
                office = request.office,
                Name = request.Name,
                key = request.key
            };

            var result = await _attendanceService.StoreAsync(attendance);
            if (!result.Success)
            {
                return CorsJson(new { success = false, message = result.Message }, StatusCodes.Status404NotFound);
            }

            return CorsJson(new { success = true, message = result.Message });
        }

        [HttpPost("clockout")]
        public async Task<JsonResult> ClockOut([FromBody] Inttendance req, [FromQuery] DateTime? day = null)
        {
            if (req.Time == default)
            {
                return CorsJson(new { success = false, message = "Invalid time" }, StatusCodes.Status400BadRequest);
            }

            var result = await _attendanceService.ClockOutAsync(req, day);
            if (!result.Success)
            {
                return CorsJson(new { success = false, message = result.Message }, StatusCodes.Status404NotFound);
            }

            return CorsJson(new { success = true, message = result.Message });
        }

        private JsonResult CorsJson(object data, int statusCode = StatusCodes.Status200OK)
        {
            Response.Headers["Access-Control-Allow-Origin"] = "*";
            Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
            Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";

            return new JsonResult(data) { StatusCode = statusCode };
        }
    }
}

