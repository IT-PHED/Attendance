using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AttendanceManager.Models
{
    [Table("attendance")]
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        [Column("staff")]
        public string Staff { get; set; } = string.Empty;

        [Column("time")]
        public DateTime Time { get; set; }

        [Column("timeout")]
        public DateTime? Timeout { get; set; }

        [Column("latitude")]
        public string Latitude { get; set; } = string.Empty;

        [Column("longitude")]
        public string Longitude { get; set; } = string.Empty;

        [Column("image")]
        public string? Image { get; set; }

        [Column("office_name")]
        public string? OfficeName { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("device_id")]
        public string? DeviceId { get; set; }

        public Inttendance ToInttendance(Attendance attendance)
        {
            return new Inttendance
            {
                Id = attendance.Staff,
                Time = attendance.Time,
                Timeout = attendance.Timeout,
                lat = decimal.TryParse(attendance.Latitude, out var latitude) ? latitude : 0m,
                @long = decimal.TryParse(attendance.Longitude, out var longitude) ? longitude : 0m,
                Image = attendance.Image,
                office = attendance.OfficeName,
                Name = attendance.Name,
                key = attendance.DeviceId ?? string.Empty
            };
        }
    }


    public class Inttendance
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Time { get; set; }
        public DateTime? Timeout { get; set; }
        public decimal lat { get; set; }
        public decimal @long { get; set; }
        public string? Image { get; set; }
        public string? office { get; set; }
        public string? Name { get; set; }
        public string key { get; set; } = string.Empty;
        
    }

    public class InttendanceFormat
    {
        public string Id { get; set; } = string.Empty;
        public String Time { get; set; } = string.Empty;
        public String? Timeout { get; set; }
        public decimal lat { get; set; }
        [JsonPropertyName("long")]
        public decimal @long { get; set; }
        public string? Image { get; set; }
        public string? office { get; set; }
        public string? Name { get; set; }
        public string key { get; set; } = string.Empty;
    }
}


