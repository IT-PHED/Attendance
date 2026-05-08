using AttendanceManager.Models;

namespace AttendanceManager.Services
{
    public class AttendanceService
    {
        private readonly DbService _db;

        public AttendanceService(DbService db)
        {
            _db = db;
        }

        public async Task<(bool Success, string Message)> StoreAsync(Inttendance request)
        {
            var lastAttendance = await _db.QuerySingleAsync<Attendance>(
                @"SELECT * FROM attendances
                  WHERE staff = @Staff
                  ORDER BY time DESC
                  LIMIT 1;",
                new { Staff = request.Id });

            if (lastAttendance is null)
            {
                await ClockInAsync(request);
                return (true, "Attendance saved successfully");
            }

            var minsDiff = (request.Time - lastAttendance.Time).TotalMinutes;
            var hoursDiff = (request.Time - lastAttendance.Time).TotalHours;

            if (minsDiff > 0 && hoursDiff <= 14)
            {
                var clockOutResult = await ClockOutAsync(request);
                return (clockOutResult.Success, clockOutResult.Message);
            }

            if (minsDiff <= 0)
            {
                await UpdateClockInAsync(request, lastAttendance);
                return (true, "Attendance updated successfully");
            }

            await ClockInAsync(request);
            return (true, "Attendance saved successfully");
        }

        public async Task<(bool Success, string Message)> ClockOutAsync(Inttendance request, DateTime? day = null)
        {
            Attendance? attendanceRecord;

            if (day.HasValue)
            {
                attendanceRecord = await _db.QuerySingleAsync<Attendance>(
                    @"SELECT * FROM attendances
                      WHERE staff = @Staff AND DATE(time) = @Day
                      ORDER BY time DESC
                      LIMIT 1;",
                    new { Staff = request.Id, Day = day.Value.Date });
            }
            else
            {
                attendanceRecord = await _db.QuerySingleAsync<Attendance>(
                    @"SELECT * FROM attendances
                      WHERE staff = @Staff
                      ORDER BY time DESC
                      LIMIT 1;",
                    new { Staff = request.Id });
            }

            if (attendanceRecord is null)
            {
                return (false, "No attendance record found.");
            }

            await _db.ExecuteAsync(
                @"UPDATE attendances
                  SET timeout = @Timeout
                  WHERE id = @Id;",
                new
                {
                    Timeout = request.Time,
                    Id = attendanceRecord.Id
                });

            return (true, "Attendance saved successfully");
        }

        private async Task ClockInAsync(Inttendance request)
        {
            await _db.ExecuteAsync(
                @"INSERT INTO attendances
                  (staff, time, latitude, longitude, image, office_name, name, device_id)
                  VALUES
                  (@Staff, @Time, @Latitude, @Longitude, @Image, @OfficeName, @Name, @DeviceId);",
                new
                {
                    Staff = request.Id,
                    Time = request.Time,
                    Latitude = request.lat.ToString(),
                    Longitude = request.@long.ToString(),
                    request.Image,
                    OfficeName = request.office,
                    Name = string.IsNullOrWhiteSpace(request.Name) || request.Name.Length <= 3 ? "N/A" : request.Name,
                    DeviceId = request.key
                });
        }

        private async Task UpdateClockInAsync(Inttendance request, Attendance lastAttendance)
        {
            await _db.ExecuteAsync(
                @"UPDATE attendances
                  SET time = @Time,
                      latitude = @Latitude,
                      longitude = @Longitude,
                      image = @Image,
                      office_name = @OfficeName,
                      name = @Name,
                      device_id = @DeviceId
                  WHERE id = @Id;",
                new
                {
                    Time = request.Time,
                    Latitude = request.lat.ToString(),
                    Longitude = request.@long.ToString(),
                    request.Image,
                    OfficeName = request.office,
                    Name = string.IsNullOrWhiteSpace(request.Name) || request.Name.Length <= 3 ? "N/A" : request.Name,
                    DeviceId = request.key,
                    Id = lastAttendance.Id
                });
        }
    }
}
