using CsvHelper;
using LogFetcher.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;
using LogMessage = LogFetcher.Models.LogMessage;

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

        [HttpGet("doc")]
        public async Task<IActionResult> GenerateDoc(string uniqueID, string? searchTerm, string type, string? fromDate, string? toDate, string docType)
        {
            await _logFetcherService.GenerateDoc(uniqueID, searchTerm, type, fromDate, toDate, docType);
            return Ok();
        }

        [HttpGet("searchTest")]
        public async Task<IActionResult> SearchLogsTest(string uniqueId, string? searchTerm, string type, string? fromDate, string? toDate)
        {
            var response = await _logFetcherService.SearchLogsTest(uniqueId, searchTerm, type, fromDate, toDate);
            return Ok(response);
        }

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


