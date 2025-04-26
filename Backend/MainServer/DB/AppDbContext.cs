using JobsClassLibrary.Classes.Job;
using Microsoft.EntityFrameworkCore;

namespace MainServer.DB
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Job> Jobs { get; set; }
        public DbSet<QueueBackupJobRow> QueueBackupJobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Setting the Primary Key
            modelBuilder.Entity<Job>()
                .HasKey(j => j.JobID);

            // Setting the Primary Key
            modelBuilder.Entity<QueueBackupJobRow>()
                .HasKey(q => q.JobID);  // JobID will be the primary key for QueueBackupJob

            // Establishing the Foreign Key Relationship
            modelBuilder.Entity<QueueBackupJobRow>()
                .HasOne<Job>()
                .WithOne()
                .HasForeignKey<QueueBackupJobRow>(q => q.JobID)
                .OnDelete(DeleteBehavior.Cascade);


            base.OnModelCreating(modelBuilder);
        }
    }
}
