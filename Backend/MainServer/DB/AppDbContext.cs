using JobsClassLibrary.Classes.Job;
using Microsoft.EntityFrameworkCore;

namespace MainServer.DB
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Job> Jobs { get; set; }
        public DbSet<QueueBackupJob> QueueBackupJobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Setting the Primary Key
            modelBuilder.Entity<Job>()
                .HasKey(j => j.JobID);

            // Setting the Primary Key
            modelBuilder.Entity<QueueBackupJob>()
                .HasKey(q => q.JobID);  // JobID will be the primary key for QueueBackupJob

            // Establishing the Foreign Key Relationship
            modelBuilder.Entity<QueueBackupJob>()
                .HasOne<Job>()
                .WithOne() 
                .HasForeignKey<QueueBackupJob>(q => q.JobID) 
                .OnDelete(DeleteBehavior.Cascade); 


            base.OnModelCreating(modelBuilder);
        }
    }
}
