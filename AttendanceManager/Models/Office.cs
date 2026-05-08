using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendanceManager.Models
{
    [Table("offices")]
    public class Office
    {
        [Key]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("address")]
        public string? Address { get; set; }

        [Column("latitude")]
        public string? Latitude { get; set; }

        [Column("longitude")]
        public string? Longitude { get; set; }
    }
}
