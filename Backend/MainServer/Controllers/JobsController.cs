using MainServer.Classes;
using MainServer.Managers;
using Microsoft.AspNetCore.Mvc;

namespace MainServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JobsController : ControllerBase
    {

        private readonly ILogger<JobsController> _logger;
        private readonly JobManager _jobManager;

        public JobsController(ILogger<JobsController> logger, JobManager jobManager)
        {
            _logger = logger;
            _jobManager = jobManager;
        }

        [HttpPost]
        public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Job name is required.");

            var job = await _jobManager.CreateJobAsync(request.Name, request.Priority);
            return CreatedAtAction(nameof(GetJobs), new { id = job.JobID }, job);
        }

        [HttpGet]
        public async Task<IActionResult> GetJobs()
        {
            var jobs = await _jobManager.GetJobsAsync();
            return Ok(jobs);
        }

        [HttpDelete("{jobID}")]
        public async Task<IActionResult> DeleteJob(string jobID)
        {
            if (!Guid.TryParse(jobID, out var guid)) return BadRequest("Invalid JobID format");

            var result = await _jobManager.DeleteJobAsync(guid);
            return result ? Ok() : NotFound();
        }

        [HttpPost("{jobID}/stop")]
        public async Task<IActionResult> StopJob(string jobID)
        {
            if (!Guid.TryParse(jobID, out var guid)) return BadRequest("Invalid JobID format");

            var result = await _jobManager.StopJobAsync(guid);
            return result ? Ok() : NotFound();
        }

        [HttpPost("{jobID}/restart")]
        public async Task<IActionResult> RestartJob(string jobID)
        {
            if (!Guid.TryParse(jobID, out var guid)) return BadRequest("Invalid JobID format");

            var result = await _jobManager.RestartJobAsync(guid);
            return result ? Ok() : NotFound();
        }
    }
}
