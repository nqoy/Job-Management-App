using JobsClassLibrary.Classes.Job;
using Microsoft.EntityFrameworkCore;

namespace MainServer.DB
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Job> Jobs { get; set; }
        public DbSet<QueueBackupJob> QueueBackups { get; set; }
    }
}
