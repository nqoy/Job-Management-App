using JobsClassLibrary.Classes;
using JobsClassLibrary.Classes.Job;
using JobsClassLibrary.Enums;
using MainServer.DB;
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
                await _eventManager.SendJobsToWorkerService([job]);


                return job;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create job with name '{JobName}' and priority {Priority}.",
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

                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching jobs. OrderBy: {OrderBy}, FilterStatus: {FilterStatus}",
                    orderBy,
                    filterStatus?.ToString() ?? "None");
                throw;
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

        public async Task<ApiResponse> StopJobAsync(Guid jobID)
        {
            Job? job;
            string message;

            try
            {
                job = await _db.Jobs.FindAsync(jobID);
            }
            catch (Exception ex)
            {
                message = "Database error while looking up job.";
                _logger.LogError(ex, " Database error while looking up JobId: {JobId}", jobID);

                return new ApiResponse
                {
                    IsSuccess = false,
                    Message = message
                };
            }
            if (job == null)
            {
                message = $"Job {jobID} not found.";
                _logger.LogWarning("Attempted to stop job {JobId}, but it was not found.", jobID);

                return new ApiResponse
                {
                    IsSuccess = false,
                    Message = message
                };
            }
            if (job.Status != JobStatus.Running && job.Status != JobStatus.InQueue)
            {
                message = $"Cannot stop job {jobID} as its status is {job.Status} and not in process.";
                _logger.LogWarning("Attempted to stop job {JobId}, but it is not in process.", jobID);

                return new ApiResponse
                {
                    IsSuccess = false,
                    Message = message
                };
            }
            try
            {
                await _eventManager.SendStopJobToWorkerService(jobID);
                message = $"Job {jobID} stopped.";
                _logger.LogDebug("Stopped job {JobId}.", jobID);

                return new ApiResponse
                {
                    IsSuccess = true,
                    Message = message
                };
            }
            catch (Exception ex)
            {
                message = "Error sending stop command or saving changes.";
                _logger.LogError(ex, message + " JobId: {JobId}", jobID);

                return new ApiResponse
                {
                    IsSuccess = false,
                    Message = message
                };
            }
        }

        public async Task<ApiResponse> RestartJobAsync(Guid jobID)
        {
            Job? job;

            try
            {
                job = await _db.Jobs.FindAsync(jobID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying job {JobId} for restart.", jobID);

                return new ApiResponse
                {
                    IsSuccess = false,
                    Message = "Database error while looking up job."
                };
            }
            if (job == null)
            {
                _logger.LogWarning("Attempted to restart job {JobId}, but it was not found.", jobID);

                return new ApiResponse
                {
                    IsSuccess = false,
                    Message = $"Job {jobID} not found."
                };
            }
            if (job.Status != JobStatus.Failed && job.Status != JobStatus.Stopped)
            {
                _logger.LogWarning("Attempted to restart job {JobId}, but its status is {Status}.",
                    jobID, job.Status);

                return new ApiResponse
                {
                    IsSuccess = false,
                    Message = $"Cannot restart job {jobID} because its status is {job.Status}."
                };
            }
            job.MarkRestarted();
            try
            {
                await _db.SaveChangesAsync();
                await _eventManager.SendJobsToWorkerService([job]);

                _logger.LogDebug("Restarted job {JobId}.", jobID);

                return new ApiResponse
                {
                    IsSuccess = true,
                    Message = $"Job {jobID} restarted.",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restart job {JobId}.", jobID);

                return new ApiResponse
                {
                    IsSuccess = false,
                    Message = "Error sending restart command or saving changes."
                };
            }
        }

        internal async Task UpdateJobProgress(Guid jobID, JobStatus updatedJobStatus, int jobProgress)
        {
            Job? job;

            try
            {
                job = await _db.Jobs.FindAsync(jobID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying job {JobId} for progress update.", jobID);
                throw;
            }

            if (job == null)
            {
                _logger.LogWarning("Attempted to update progress for job {JobId}, but it was not found.", jobID);
                throw new InvalidOperationException($"Job {jobID} not found.");
            }
            JobProgress jobProgression = new JobProgress()
            {
                JobID = jobID,
                Status = updatedJobStatus,
                Progress = jobProgress
            };

            await _eventManager.SendJobProgressUpdateToJobsApp(jobProgression);


            if (job.Status != updatedJobStatus)
            {
                switch (updatedJobStatus)
                {
                    case JobStatus.Running:
                        job.MarkStarted();
                        break;

                    case JobStatus.Completed:
                        job.MarkCompleted();
                        break;
                    case JobStatus.InQueue:
                        job.Status = updatedJobStatus;
                        break;
                    default:
                        job.MarkProgress(updatedJobStatus, jobProgress);
                        break;
                }
                try
                {
                    await _db.SaveChangesAsync();
                    _logger.LogDebug("Updated job [{JobId}] progress :\n [{JobStatus}]  [{JobProgress}%].", jobID, updatedJobStatus, jobProgress);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update progress for job [{JobId}].", jobID);
                    throw;
                }
                try
                {
                    // await _eventManager.SendJobProgressUpdateToJobsApp(jobID, updatedJobStatus);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send progress update for job [{JobId}] via event manager.", jobID);
                    throw;
                }
            }
        }

        internal async Task<int> DeleteJobsByStatusAsync(JobStatus status)
        {
            try
            {
                int deletedCount = await _db.Jobs
                    .Where(job => job.Status == status)
                    .ExecuteDeleteAsync();

                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting jobs with status {Status}.", status);
                throw;
            }
        }

        internal async Task SaveQueueBackupData(List<QueueBackupJob> queuedJobs)
        {
            if (queuedJobs == null)
            {
                _logger.LogInformation("No queued jobs to back up.");
                return;
            }

            long backupTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            try
            {
                List<QueueBackupJobRow> backupEntries = queuedJobs.Select(job => new QueueBackupJobRow
                {
                    JobID = job.JobID,
                    Priority = job.Priority,
                    QueuingTime = job.QueuingTime,
                    BackupTimestamp = backupTimestamp
                }).ToList();

                _db.QueueBackupJobs.RemoveRange(_db.QueueBackupJobs);
                _db.QueueBackupJobs.AddRange(backupEntries);

                await _db.SaveChangesAsync();

                _logger.LogDebug("Cleared all previous backups and saved {Count} new entries at timestamp {Timestamp}.", backupEntries.Count, backupTimestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save queue backup data.");
            }
        }

        internal async Task SendRecoveryQueuedJobsToWorkerService()
        {
            try
            {
                List<QueuedJob> queuedJobs = await (
                    from backup in _db.QueueBackupJobs
                    join job in _db.Jobs on backup.JobID equals job.JobID
                    select new QueuedJob
                    {
                        JobID = job.JobID,
                        Priority = job.Priority,
                        Status = JobStatus.Pending,
                        QueuingTime = backup.QueuingTime
                    }).ToListAsync();

                List<QueuedJob> runningJobs = await (
                    from job in _db.Jobs
                    where job.Status == JobStatus.Running
                    select new QueuedJob
                    {
                        JobID = job.JobID,
                        Priority = job.Priority,
                        Status = JobStatus.Pending,
                        QueuingTime = 0
                    }).ToListAsync();

                // To avoid duplication of "Running" jobs already in QueueBackupJobs in case of an app crash.
                // Backup occurs periodically and during system shutdown.
                HashSet<Guid> existingJobIds = queuedJobs.Select(q => q.JobID).ToHashSet();

                List<QueuedJob> filteredRunningJobs = runningJobs
                    .Where(r => !existingJobIds.Contains(r.JobID))
                    .ToList();

                List<QueuedJob> combinedJobs = queuedJobs
                    .Concat(filteredRunningJobs)
                    .OrderBy(j => j.Priority)
                    .ThenBy(j => j.QueuingTime)
                    .ToList();

                List<Job> jobsToRun = combinedJobs.Cast<Job>().ToList();

                if (jobsToRun.Count == 0)
                    return;
                
                await _eventManager.SendJobsToWorkerService(jobsToRun);

                _logger.LogDebug("Sent {Count} jobs to worker service.", combinedJobs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch and send queued jobs to worker service.");
            }
        }
    }
}