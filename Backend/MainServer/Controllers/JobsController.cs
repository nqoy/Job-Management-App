using JobsClassLibrary.Classes;
using MainServer.Classes;
using MainServer.Managers;
using Microsoft.AspNetCore.Mvc;

namespace MainServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JobsController(ILogger<JobsController> logger, JobManager jobManager) : ControllerBase
    {
        private readonly ILogger<JobsController> _logger = logger;
        private readonly JobManager _jobManager = jobManager;

        [HttpPost]
        public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                _logger.LogWarning("Bad request: Job name is required. Request: {@Request}", request);

                return BadRequest("Job name is required.");
            }

            try
            {
                Job? job = await _jobManager.CreateJobAsync(request.Name, request.Priority);

                if (job == null)
                {
                    _logger.LogWarning("Failed to create job. Request: {@Request}", request);

                    return StatusCode(500, "An error occurred while creating the job.");
                }

                _logger.LogDebug("Job created successfully. Job: {@Job}", job);

                return Ok(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job. Request: {@Request}", request);

                return StatusCode(500, "An error occurred while creating the job.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetJobs()
        {
            try
            {
                List<Job>? jobs = await _jobManager.GetJobsAsync();

                _logger.LogInformation("Successfully retrieved jobs. Jobs count: {JobCount}", jobs?.Count);

                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs.");

                return StatusCode(500, "An error occurred while retrieving jobs.");
            }
        }

        [HttpDelete("{jobID}")]
        public async Task<IActionResult> DeleteJob(string jobID)
        {
            if (!Guid.TryParse(jobID, out var guid))
            {
                _logger.LogWarning("Bad request: Invalid JobID format. Provided JobID: {JobID}", jobID);

                return BadRequest("Invalid JobID format");
            }

            try
            {
                bool isSuccess = await _jobManager.DeleteJobAsync(guid);

                if (isSuccess)
                {
                    _logger.LogInformation("Job deleted successfully. JobID: {JobID}", guid);

                    return Ok();
                }
                else
                {
                    _logger.LogWarning("Job not found for deletion. JobID: {JobID}", guid);

                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job. JobID: {JobID}", guid);

                return StatusCode(500, "An error occurred while deleting the job.");
            }
        }

        [HttpPost("{jobID}/stop")]
        public async Task<IActionResult> StopJob(string jobID)
        {
            if (!Guid.TryParse(jobID, out var guid))
            {
                _logger.LogWarning("Bad request: Invalid JobID format. Provided JobID: {JobID}", jobID);

                return BadRequest("Invalid JobID format");
            }

            try
            {
                bool isSuccess = await _jobManager.StopJobAsync(guid);

                if (isSuccess)
                {
                    _logger.LogInformation("Job stopped successfully. JobID: {JobID}", guid);

                    return Ok();
                }
                else
                {
                    _logger.LogWarning("Job not found for stopping. JobID: {JobID}", guid);

                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping job. JobID: {JobID}", guid);

                return StatusCode(500, "An error occurred while stopping the job.");
            }
        }

        [HttpPost("{jobID}/restart")]
        public async Task<IActionResult> RestartJob(string jobID)
        {
            if (!Guid.TryParse(jobID, out Guid guid))
            {
                _logger.LogWarning("Bad request: Invalid JobID format. Provided JobID: {JobID}", jobID);

                return BadRequest("Invalid JobID format");
            }

            try
            {
                bool isSuccess = await _jobManager.RestartJobAsync(guid);

                if (isSuccess)
                {
                    _logger.LogInformation("Job restarted successfully. JobID: {JobID}", guid);

                    return Ok();
                }
                else
                {
                    _logger.LogWarning("Job not found for restarting. JobID: {JobID}", guid);

                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting job. JobID: {JobID}", guid);

                return StatusCode(500, "An error occurred while restarting the job.");
            }
        }
    }
}
