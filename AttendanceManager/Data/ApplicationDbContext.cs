using AttendanceManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManager.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<Office> Offices => Set<Office>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Attendance>().ToTable("attendances");
            modelBuilder.Entity<Office>().ToTable("offices");
            modelBuilder.Entity<User>().ToTable("users");
        }
    }
}
