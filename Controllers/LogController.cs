using LogFetcher.Services.Implementation;
using LogFetcher.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace LogFetcher.Controllers
{
    [ApiController]
    [Route("api")]
    public class LogController : ControllerBase
    {
        private readonly ILogger<LogController> _logger;
        private readonly ILogFetcherService _logFetcherService;
        public LogController(ILogger<LogController> logger, ILogFetcherService logFetcherService)
        {
            _logger = logger;
            _logFetcherService = logFetcherService;
        }

        [HttpGet("checkUniqueId")]
        public async Task<IActionResult> CheckUniqueId(string uniqueId)
        {
            var response = await _logFetcherService.CheckUniqueId(uniqueId);
            return Ok(new { response });
        }

        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts(string uniqueId)
        {
            var response = await _logFetcherService.GetAlerts(uniqueId);
            return Ok(response);
        }

        //[HttpGet("logs")]
        //public async Task<IActionResult> GetLogs(string uniqueId)
        //{
        //    var response = await _logFetcherService.GetLogs(uniqueId);
        //    return Ok(response);
        //}

        //[HttpGet("searchLogs")]
        //public async Task<IActionResult> SearchLogs(string uniqueId, string searchTerm)
        //{
        //    // Construct the Elasticsearch query based on the search term and any other parameters
        //    var response = await _logFetcherService.SearchLogs(uniqueId, searchTerm);
        //    return Ok(response);
        //    // Process the search response and extract relevant log data
        //}

        //[HttpGet("search")]
        //public async Task<IActionResult> SearchLogsBasedOnLevel(string uniqueId, string level)
        //{
        //    var response = await _logFetcherService.SearchLogsBasedOnLevel(uniqueId, level);
        //    return Ok(response);
        //}

        [HttpGet("searchTest")]
        public async Task<IActionResult> SearchLogsTest(string uniqueId, string? searchTerm, string type, string? fromDate, string? toDate)
        {
            var response = await _logFetcherService.SearchLogsTest(uniqueId, searchTerm, type, fromDate, toDate);
            return Ok(response);
        }

        //[HttpGet("searchLogsTimeRange")]
        //public async Task<IActionResult> SearchLogsBasedOnTimeRange(string uniqueId, string from, string to)
        //{
        //    var response = await _logFetcherService.SearchLogsBasedOnTimeRange(uniqueId, from, to);
        //    return Ok(response);
        //}

        [HttpGet("visualisationLogs")]
        public async Task<ActionResult<Dictionary<string, int>>> GetLogsForVisualize(string uniqueId)
        {
            var response = await _logFetcherService.GetLogsForVisualize(uniqueId);
            return Ok(response);
        }

        [HttpGet("visualisationLogsForLineGraph")]
        public async Task<ActionResult<Dictionary<string, int>>> GetLogsForVisualizeForLineGraph(string uniqueId)
        {
            var response = await _logFetcherService.GetLogsForVisualizeForLineGraph(uniqueId);
            return Ok(response);
        }

        [HttpGet("visualisationLogsForLineGraphError")]
        public async Task<ActionResult<Dictionary<string, int>>> GetLogsForVisualizeForLineGraphError(string uniqueId)
        {
            var response = await _logFetcherService.GetLogsForVisualizeForLineGraphError(uniqueId);
            return Ok(response);
        }
    }
}


