using Microsoft.AspNetCore.Mvc;
using Nest;
using System.Text;
using System.Xml;

namespace LogFetcher.Controllers
{
    [ApiController]
    [Route("api")]
    public class LogController : ControllerBase
    {
        private readonly ILogger<LogController> _logger;
        private readonly IElasticClient _elasticClient;
        public LogController(ILogger<LogController> logger)
        {
            _logger = logger;
            _elasticClient = CreateElasticClient();
        }

        [HttpGet("checkUniqueId")]
        public async Task<IActionResult> CheckUniqueId(string uniqueId)
        {
            var response = await _elasticClient.Indices.ExistsAsync(uniqueId);

            bool exists = response.Exists;
            return Ok(new { exists });
        }

        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts(string uniqueId)
        {
            var searchResponse = await _elasticClient.SearchAsync<LogMessage>(s => s
        .Index(uniqueId)
        .Query(q => q
            .Bool(b => b
                .Should(
                    bs => bs.Regexp(r => r.Field(f => f.Message).Value(".*\\[Level:(Information|Error)\\].*"))
                )
            )
        )
    );

            return Ok(searchResponse.Documents.ToList());
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs(string uniqueId)
        {
            List<LogMessage> logMsgs = new();
            var scrollResponse = await _elasticClient.SearchAsync<LogMessage>(s => s
                                    .Index(uniqueId)
                                    .Size(2) // Set the initial batch size
                                    .Scroll("1m") // Set the scroll time window
                                    );
            while (scrollResponse.Documents.Any())
            {
                logMsgs.AddRange(scrollResponse.Documents);

                // Get the next batch using the scroll ID
                scrollResponse = await _elasticClient.ScrollAsync<LogMessage>("1m", scrollResponse.ScrollId);
            }
            //var searchResponse = await _elasticClient.SearchAsync<LogMessage>(s => s
            //    .Index(uniqueId).Size(1000)
            //);

            //var logs = searchResponse.Documents.OrderBy(x => x.TimeStamp).Select(x => new LogMessage { Level = GetType(x.Level), Message = x.Message, TimeStamp = x.TimeStamp }).ToList();
            return Ok(logMsgs.OrderBy(x => x.TimeStamp).Select(x => new LogMessage { Level = GetType(x.Level), Message = x.Message, TimeStamp = x.TimeStamp }).ToList());
        }

        private static string GetType(string level)
        {
            return level switch
            {
                "Information" => "info",
                "Debug" => "success",
                "Critical" => "error",
                "Warning" => "warning",
                "Error" => "error",
                _ => "info",
            };
        }

        [HttpGet("searchLogs")]
        public async Task<IActionResult> SearchLogs(string uniqueId, string searchTerm)
        {
            // Construct the Elasticsearch query based on the search term and any other parameters
            try
            {
                if (string.IsNullOrEmpty(searchTerm))
                {
                    return await GetLogs(uniqueId);
                }
                else
                {
                    var searchResponse = await _elasticClient.SearchAsync<LogMessage>(s => s.Index(uniqueId)
                        .Query(q => q.MultiMatch(x => x.Fields(z => z.Field(e => e.Level).Field(e => e.Message)).Query(searchTerm))
                        //.Match(m => m
                        //    .Field(f => f.Level)
                        //    .Query(searchTerm)
                        //)
                        ).Highlight(h => h
                .Fields(f => f
                    .Field(ff => ff.Message).Field(x => x.Level)
                )
            )
                    // Add other search parameters and aggregations as needed
                    );
                    var logs = searchResponse.Hits.Select(h => h.Source);
                    return Ok(logs);
                }
            }
            catch (Exception e)
            {

            }
            return Ok();
            // Process the search response and extract relevant log data

        }

        [HttpGet("visualisationLogs")]
        public async Task<ActionResult<IEnumerable<LogDto>>> GetLogsForVisualize(string uniqueId)
        {
            var searchResponse = await _elasticClient.SearchAsync<LogMessage>(s => s
                 .Index(uniqueId)
             );

            var logMessages = searchResponse.Documents.OrderBy(x => x.TimeStamp).ToList();
            var logs = new List<LogDto>();

            foreach (var logMessage in logMessages)
            {
                var level = ExtractLogLevel(logMessage.Message);
                var logDto = new LogDto
                {
                    Level = level,
                    Message = logMessage.Message
                };
                logs.Add(logDto);
            }

            return Ok(logs);
        }

        private string ExtractLogLevel(string logMessage)
        {
            if (logMessage.Contains("Level:Information"))
            {
                return "Information";
            }
            else if (logMessage.Contains("Level:Error"))
            {
                return "Error";
            }
            else if (logMessage.Contains("Level:Warning"))
            {
                return "Warning";
            }
            else if (logMessage.Contains("Level:Debug"))
            {
                return "Debug";
            }
            else if (logMessage.Contains("Level:Critical"))
            {
                return "Critical";
            }

            return "Unknown";
        }


        private static IElasticClient CreateElasticClient()
        {
            var connectionString = "http://localhost:9200/";
            var settings = new ConnectionSettings(new Uri(connectionString));
            var elasticClient = new ElasticClient(settings);
            return elasticClient;
        }
    }

    public class LogMessage
    {
        public string? Level { get; set; }
        public string? Message { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    public class LogDto
    {
        public string Level { get; set; }
        public string Message { get; set; }
    }
}


