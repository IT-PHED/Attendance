using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendanceManager.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("staff_number")]
        public string StaffNumber { get; set; } = string.Empty;

        [Column("department")]
        public string? Department { get; set; }

        [Column("designation")]
        public string? Designation { get; set; }

        [Column("location")]
        public string? Location { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("updated_by")]
        public string? UpdatedBy { get; set; }

        [Column("password")]
        public string? Password { get; set; }

        [Column("remember_token")]
        public string? RememberToken { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
