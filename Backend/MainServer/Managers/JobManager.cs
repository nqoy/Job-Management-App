using JobsClassLibrary.Classes;
using JobsClassLibrary.Enums;
using MainServer.Classes;
using Microsoft.EntityFrameworkCore;

namespace MainServer.Managers
{
    public class JobManager(AppDbContext db, ILogger<JobManager> logger, JobEventManager eventManager)
    {
        private readonly AppDbContext _db = db;
        private readonly ILogger<JobManager> _logger = logger;
        private readonly JobEventManager _eventManager = eventManager;

        public async Task<Job?> CreateJobAsync(string name, JobPriority priority)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("CreateJobAsync called with empty name.");
                return null;
            }
            Job job = new()
            {
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Status = JobStatus.Pending,
                JobID = Guid.NewGuid(),
                Priority = priority,
                Progress = 0,
                Name = name
            };

            try
            {
                _db.Jobs.Add(job);
                await _db.SaveChangesAsync();

                _logger.LogDebug("Created job {JobId} (Name: {JobName}, Priority: {Priority})",
                    job.JobID, job.Name, job.Priority);

                List<Job> singleJobList = [job];

                await _eventManager.SendJobsToWorkerService(singleJobList);

                return job;
            }
            catch (Exception ex)
            {
                _logger.LogError( ex, "Failed to create job with name '{JobName}' and priority {Priority}.",
                    name, priority);

                return null;
            }
        }

        public async Task<List<Job>?> GetJobsAsync(JobIQuery orderBy = JobIQuery.CreatedAt, JobStatus? filterStatus = null)
        {
            _logger.LogDebug(
                "Fetching jobs. OrderBy: {OrderBy}, FilterStatus: {FilterStatus}",
                orderBy,
                filterStatus?.ToString() ?? "None");

            try
            {
                IQueryable<Job> jobsQuery = _db.Jobs;

                if (filterStatus.HasValue)
                {
                    jobsQuery = jobsQuery.Where(job => job.Status == filterStatus.Value);
                }

                switch (orderBy)
                {
                    case JobIQuery.Name:
                        jobsQuery = jobsQuery.OrderBy(job => job.Name);
                        break;
                    case JobIQuery.Priority:
                        jobsQuery = jobsQuery.OrderBy(job => job.Priority);
                        break;
                    case JobIQuery.Status:
                        jobsQuery = jobsQuery.OrderBy(job => job.Status);
                        break;
                    default:
                        jobsQuery = jobsQuery.OrderByDescending(job => job.CreatedAt);
                        break;
                }

                List<Job> jobs = await jobsQuery.ToListAsync();

                _logger.LogDebug("Retrieved {Count} jobs.", jobs.Count);

                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching jobs. OrderBy: {OrderBy}, FilterStatus: {FilterStatus}",
                    orderBy,
                    filterStatus?.ToString() ?? "None");
                return null;
            }
        }

        public async Task<bool> DeleteJobAsync(Guid jobID)
        {
            Job? job;

            try
            {
                job = await _db.Jobs.FindAsync(jobID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying job {JobId} for deletion.", jobID);

                return false;
            }

            if (job == null)
            {
                _logger.LogWarning("Attempted to delete job {JobId}, but it was not found.", jobID);

                return false;
            }

            try
            {
                _db.Jobs.Remove(job);
                await _db.SaveChangesAsync();

                _logger.LogDebug("Deleted job {JobId}.", jobID);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete job {JobId}.", jobID);

                return false;
            }
        }

        public async Task<bool> StopJobAsync(Guid jobID)
        {
            Job? job;

            try
            {
                job = await _db.Jobs.FindAsync(jobID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying job {JobId} for stop.", jobID);
                return false;
            }

            if (job == null)
            {
                _logger.LogWarning("Attempted to stop job {JobId}, but it was not found.", jobID);
                return false;
            }

            job.Status = JobStatus.Stopped;

            try
            {
                List<Guid> stopList = [jobID];

                await _eventManager.SendStopJobToWorkerService(stopList);
                await _db.SaveChangesAsync();

                _logger.LogDebug("Stopped job {JobId}.", jobID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop job {JobId}.", jobID);
                return false;
            }
        }

        public async Task<bool> RestartJobAsync(Guid jobID)
        {
            Job? job;

            try
            {
                job = await _db.Jobs.FindAsync(jobID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying job {JobId} for restart.", jobID);
                return false;
            }
            if (job == null)
            {
                _logger.LogWarning("Attempted to restart job {JobId}, but it was not found.", jobID);
                return false;
            }
            job.Status = JobStatus.Pending;
            job.Progress = 0;
            job.StartedAt = -1;
            job.CompletedAt = -1;

            try
            {
                await _db.SaveChangesAsync();

                List<Job> restartList = [job];

                await _eventManager.SendJobsToWorkerService(restartList);

                _logger.LogDebug("Restarted job {JobId}.", jobID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restart job {JobId}.", jobID);
                return false;
            }
        }

        internal async Task UpdateJobStatusAsync(dynamic jobIds, dynamic? jobStatus)
        {
            throw new NotImplementedException();
        }
    }
}