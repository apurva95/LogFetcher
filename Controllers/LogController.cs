using Microsoft.AspNetCore.Mvc;
using Nest;
using System.Globalization;
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

        [HttpPost("search")]
        public async Task<IActionResult> SearchLogs([FromBody] LogSearchRequest request)
        {
            try
            {
                List<LogMessage> logMsgs = new();
                // Construct the search query based on selected buckets
                //var query = new BoolQuery();

                ////// Add time range filter
                //var timeFilter = new BoolQuery();
                //foreach (var interval in request.SelectedIntervals)
                //{
                //    var intervalStart = interval.Split('-')[0].Trim();
                //    var intervalEnd = interval.Split('-')[1].Trim();

                //    var rangeQuery = new DateRangeQuery
                //    {
                //        Field = "TimeStamp",
                //        GreaterThanOrEqualTo = intervalStart,
                //        LessThanOrEqualTo = intervalEnd
                //    };

                //    timeFilter.Should = new QueryContainer[] { rangeQuery };
                //}

                //// Add level filter
                //var processedLevels = request.SelectedLevels.Select(l => l.ToLower());
                //var levelFilter = new TermsQuery
                //{
                //    Field = "level",
                //    Terms = processedLevels // Assuming the selected level doesn't contain additional colons or brackets
                //};

                //query.Filter = new List<QueryContainer> { timeFilter, levelFilter };

                //// Create the search descriptor
                //var searchDescriptor = new SearchDescriptor<LogMessage>()
                //    .Index("deloitte_usi-ui-wage-2c006383-679d-4148-87ce-a6b722c41734") // Replace "your_index_name" with the actual index name
                //    .Query(q => query);
                var scrollResponse = _elasticClient.Search<LogMessage>(s => s.Index(request.RegistrationId)
        .Query(q => q
            .Terms(t => t
                .Field(f => f.Level)
                .Terms(request.SelectedLevels.Select(l => l.ToLower()))
            )
        ).Size(2) // Set the initial batch size
                                    .Scroll("1m") // Set the scroll time window
    );
                while (scrollResponse.Documents.Any())
                {
                    logMsgs.AddRange(scrollResponse.Documents);

                    // Get the next batch using the scroll ID
                    scrollResponse = await _elasticClient.ScrollAsync<LogMessage>("1m", scrollResponse.ScrollId);
                }
                // Perform the search request
                //var searchResponse1 = await _elasticClient.SearchAsync<LogMessage>(searchDescriptor);

                // Process the search results and return them
                //var logs = searchResponse.Hits.Select(hit => hit.Source);
                return Ok(logMsgs.OrderBy(x => x.TimeStamp).Select(x => new LogMessage { Level = GetType(x.Level), Message = x.Message, TimeStamp = x.TimeStamp }).ToList());
            }
            catch (Exception ex)
            {
                // Handle the exception here
            }
            return Ok();
        }

        [HttpGet("visualisationLogs")]
        public async Task<ActionResult<Dictionary<string, int>>> GetLogsForVisualize(string uniqueId)
        {
            List<LogMessage> logMsgs = new();
            var scrollResponse = await _elasticClient.SearchAsync<LogMessage>(s => s
                 .Index(uniqueId).Size(2) // Set the initial batch size
                                    .Scroll("1m") // Set the scroll time window
             );

            while (scrollResponse.Documents.Any())
            {
                logMsgs.AddRange(scrollResponse.Documents);

                // Get the next batch using the scroll ID
                scrollResponse = await _elasticClient.ScrollAsync<LogMessage>("1m", scrollResponse.ScrollId);
            }

            var logMessages = logMsgs.OrderBy(x => x.TimeStamp).ToList();
            var dict = new Dictionary<string, int>();
            foreach (var logMessage in logMessages)
            {
                var level = ExtractLogLevel(logMessage.Message);
                if (!dict.ContainsKey(level))
                {
                    dict.Add(level, 1);
                }
                else
                {
                    dict[level]++;
                }
            }

            return Ok(dict);
        }

        [HttpGet("visualisationLogsForLineGraph")]
        public async Task<ActionResult<Dictionary<string, int>>> GetLogsForVisualizeForLineGraph(string uniqueId)
        {
            try
            {
                List<LogMessage> logMsgs = new();
                var scrollResponse = await _elasticClient.SearchAsync<LogMessage>(s => s
                     .Index(uniqueId).Size(2) // Set the initial batch size
                                    .Scroll("1m")
                 );

                while (scrollResponse.Documents.Any())
                {
                    logMsgs.AddRange(scrollResponse.Documents);

                    // Get the next batch using the scroll ID
                    scrollResponse = await _elasticClient.ScrollAsync<LogMessage>("1m", scrollResponse.ScrollId);
                }

                var logMessages = logMsgs.OrderBy(x => x.TimeStamp).ToList();
                var dict = new Dictionary<string, int>();

                // Get the timestamp of the first log
                var firstLogTime = logMessages.First().TimeStamp;

                // Get the timestamp of the most recent log
                var recentLogTime = logMessages.Last().TimeStamp;

                // Initialize the start time at the first log timestamp
                var startTime = new DateTime(firstLogTime.Year, firstLogTime.Month, firstLogTime.Day, 0, 0, 0);

                // Iterate until the current time reaches or exceeds the recent log timestamp
                while (startTime <= recentLogTime)
                {
                    // Calculate the end time for the current day
                    var endTime = startTime.AddDays(1);

                    // Format the day range as "YYYY-MM-dd"
                    var dayRange = startTime.ToString("dd-MM-yyyy");

                    // Count the number of logs within the day range
                    var logCount = logMessages.Count(logMessage => logMessage.TimeStamp >= startTime && logMessage.TimeStamp < endTime);

                    // Add the day range and log count to the dictionary
                    dict.Add(dayRange, logCount);

                    // Update the start time for the next iteration
                    startTime = endTime;
                }

                return Ok(dict);
            }
            catch(Exception e)
            {

            }
            return Ok();
        }

        [HttpGet("visualisationLogsForLineGraphError")]
        public async Task<ActionResult<Dictionary<string, int>>> GetLogsForVisualizeForLineGraphError(string uniqueId)
        {
            try
            {
                List<LogMessage> logMsgs = new();
                var scrollResponse = await _elasticClient.SearchAsync<LogMessage>(s => s
                    .Index(uniqueId)
                    .Size(2) // Set the initial batch size
                    .Scroll("1m")
                );

                while (scrollResponse.Documents.Any())
                {
                    logMsgs.AddRange(scrollResponse.Documents);

                    // Get the next batch using the scroll ID
                    scrollResponse = await _elasticClient.ScrollAsync<LogMessage>("1m", scrollResponse.ScrollId);
                }

                var logMessages = logMsgs.OrderBy(x => x.TimeStamp).ToList();
                var dict = new Dictionary<string, int>();

                // Get the timestamp of the first log
                var firstLogTime = logMessages.First().TimeStamp;

                // Get the timestamp of the most recent log
                var recentLogTime = logMessages.Last().TimeStamp;

                // Initialize the start time at the first log timestamp
                var startTime = new DateTime(firstLogTime.Year, firstLogTime.Month, firstLogTime.Day, 0, 0, 0);

                // Iterate until the current time reaches or exceeds the recent log timestamp
                while (startTime <= recentLogTime)
                {
                    // Calculate the end time for the current time slot (3 hours later)
                    var endTime = startTime.AddHours(3);

                    // Format the time slot range
                    var timeSlotRange = $"{startTime:dd-MM-yyyy HH:mm} - {endTime:dd-MM-yyyy HH:mm}";

                    // Count the number of error logs within the time slot range
                    var errorCount = logMessages.Count(logMessage => logMessage.TimeStamp >= startTime && logMessage.TimeStamp < endTime && (logMessage.Level == "Error" || logMessage.Level == "Critical"));

                    // Add the time slot range and error count to the dictionary
                    dict.Add(timeSlotRange, errorCount);

                    // Update the start time for the next time slot
                    startTime = endTime;
                }

                return Ok(dict);
            }
            catch (Exception e)
            {
                // Handle any exceptions
            }

            return Ok();
        }

        private string ExtractLogLevel(string logMessage)
        {
            if (logMessage.Contains("Level:Information") || logMessage.Contains("Level: Information"))
            {
                return "Information";
            }
            else if (logMessage.Contains("Level:Error") || logMessage.Contains("Level: Error"))
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

    public class LogSearchRequest
    {
        public string RegistrationId { get; set; }
        public List<string> SelectedLevels { get; set; }
        public List<string> SelectedIntervals { get; set; }
    }
}


