using JobsClassLibrary.Classes.Job;
using Microsoft.EntityFrameworkCore;

namespace MainServer.DB
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Job> Jobs { get; set; }
        public DbSet<QueueBackupJob> QueueBackupJobs { get; set; }

        // Override OnModelCreating to configure entity mappings
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Job>()
                .HasKey(j => j.JobID);

            modelBuilder.Entity<QueueBackupJob>()
                .HasKey(q => q.JobID);

            base.OnModelCreating(modelBuilder);
        }
    }
}
