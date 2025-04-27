using JobsClassLibrary.Classes;
using JobsClassLibrary.Classes.Job;
using JobsClassLibrary.Enums;
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

                _logger.LogDebug("Job created. Job: {@Job}", job);

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

                _logger.LogDebug("Successfully retrieved jobs. Jobs count: {JobCount}", jobs?.Count);

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
                    _logger.LogDebug("Job deleted. JobID: {JobID}", guid);

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

        [HttpDelete("status/{status}")]
        public async Task<IActionResult> DeleteJobsByStatus(string status)
        {
            if (!Enum.TryParse(status, true, out JobStatus parsedStatus))
            {
                _logger.LogWarning("Invalid status: {Status}. The status is not a valid JobStatus enum value.", status);
                return base.BadRequest("Invalid status. The status must be a valid JobStatus enum value.");
            }

            if (parsedStatus != JobStatus.Failed && parsedStatus != JobStatus.Completed)
            {
                _logger.LogWarning("Invalid status: {Status}. Only 'Failed' or 'Completed' are allowed.", status);
                return base.BadRequest("Invalid status. Only 'Failed' or 'Completed' statuses are allowed.");
            }

            try
            {
                int deletedCount = await _jobManager.DeleteJobsByStatusAsync(parsedStatus);

                if (deletedCount > 0)
                {
                    _logger.LogDebug("{DeletedCount} jobs with status {Status} deleted.", deletedCount, parsedStatus);
                    return Ok($"{deletedCount} jobs deleted.");
                }
                else
                {
                    return NoContent();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting jobs with status {Status}.", status);
                return StatusCode(500, $"An error occurred while deleting jobs : {ex.Message}");
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
                ApiResponse response = await _jobManager.StopJobAsync(guid);

                if (response.IsSuccess)
                {
                    _logger.LogDebug("Job stopped. JobID: {JobID}", guid);

                    return Ok(response);
                }
                if (response.Message?.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0)
                    return NotFound(response);

                if (response.Message?.IndexOf("cannot stop job", StringComparison.OrdinalIgnoreCase) >= 0)
                    return Conflict(response);

                return StatusCode(500, response);
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
            if (!Guid.TryParse(jobID, out var guid))
            {
                _logger.LogWarning("Bad request: Invalid JobID format. Provided JobID: {JobID}",
                    jobID);

                return BadRequest(new ApiResponse
                {
                    IsSuccess = false,
                    Message = "Invalid JobID format."
                });
            }

            ApiResponse response;

            try
            {
                response = await _jobManager.RestartJobAsync(guid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error restarting job. JobID: {JobID}", guid);

                return StatusCode(500, new ApiResponse
                {
                    IsSuccess = false,
                    Message = "An unexpected error occurred while restarting the job."
                });
            }

            if (response.IsSuccess)
            {
                _logger.LogDebug("Job restarted. JobID: {JobID}", guid);

                return Ok(response);
            }

            if (response.Message!.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(response);

            if (response.Message!.StartsWith("Cannot restart job", StringComparison.OrdinalIgnoreCase))
                return Conflict(response);

            return StatusCode(500, response);
        }
    }
}
