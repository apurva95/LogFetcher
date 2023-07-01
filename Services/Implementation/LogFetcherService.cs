using LogFetcher.Models;
using LogFetcher.Services.Interface;
using Nest;
using System.Globalization;

namespace LogFetcher.Services.Implementation
{
    public class LogFetcherService : ILogFetcherService
    {
        private readonly IElasticClient _elasticClient;

        public LogFetcherService()
        {
            _elasticClient = CreateElasticClient();
        }
        private static IElasticClient CreateElasticClient()
        {
            var connectionString = "http://localhost:9200/";
            var settings = new ConnectionSettings(new Uri(connectionString));
            var elasticClient = new ElasticClient(settings);
            return elasticClient;
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

        public async Task<bool> CheckUniqueId(string uniqueId)
        {
            var response = await _elasticClient.Indices.ExistsAsync(uniqueId);
            bool exists = response.Exists;
            return exists;
        }

        public async Task<List<LogMessage>> GetLogs(string uniqueId)
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
            return logMsgs.OrderBy(x => x.TimeStamp).Select(x => new LogMessage { Level = GetType(x.Level), Message = x.Message, TimeStamp = x.TimeStamp }).ToList();
        }

        public async Task<List<LogMessage>> SearchLogs(string uniqueId, string searchTerm)
        {
            try
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
                return logs.OrderBy(x => x.TimeStamp).Select(x => new LogMessage { Level = GetType(x.Level), Message = x.Message, TimeStamp = x.TimeStamp }).ToList();

            }
            catch (Exception e)
            {
                return new();

            }
        }

        public async Task<List<LogMessage>> SearchLogsBasedOnLevel(string uniqueId, string level)
        {
            try
            {
                List<LogMessage> logMsgs = new();
                ISearchResponse<LogMessage> scrollResponse;
                if (level == "All")
                {
                    scrollResponse = await _elasticClient.SearchAsync<LogMessage>(s => s
                                    .Index(uniqueId)
                                    .Size(2) // Set the initial batch size
                                    .Scroll("1m") // Set the scroll time window
                                    );
                }
                else
                {
                    scrollResponse = await _elasticClient.SearchAsync<LogMessage>(s => s.Index(uniqueId)
            .Query(q => q
                .Terms(t => t
                    .Field(f => f.Level)
                    .Terms(new List<string> { level.ToLower() })
                )
            ).Size(2) // Set the initial batch size
                                        .Scroll("1m") // Set the scroll time window
        );
                }
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
                return logMsgs.OrderBy(x => x.TimeStamp).Select(x => new LogMessage { Level = GetType(x.Level), Message = x.Message, TimeStamp = x.TimeStamp }).ToList();
            }
            catch (Exception ex)
            {
                return new();
            }
        }

        public async Task<Dictionary<string, int>> GetLogsForVisualize(string uniqueId)
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
            return dict;
        }

        public async Task<Dictionary<string, int>> GetLogsForVisualizeForLineGraph(string uniqueId)
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

                return dict;
            }
            catch (Exception e)
            {
                return new();
            }
        }

        public async Task<Dictionary<string, int>> GetLogsForVisualizeForLineGraphError(string uniqueId)
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

                return dict;
            }
            catch (Exception e)
            {
                return new();
            }
        }

        public async Task<List<LogMessage>> SearchLogsBasedOnTimeRange(string uniqueId, string from, string to)
        {
            try
            {
                List<LogMessage> logMsgs = new();
                ISearchResponse<LogMessage> scrollResponse = await _elasticClient.SearchAsync<LogMessage>(s => s
     .Index(uniqueId)
     .Query(q => q
         .DateRange(dr => dr
             .Field(f => f.TimeStamp)
             .GreaterThanOrEquals(Convert.ToDateTime(from))
             .LessThanOrEquals(Convert.ToDateTime(to))
         )
     )
     .Size(2) // Set the initial batch size
     .Scroll("1m") // Set the scroll time window
 );

                while (scrollResponse.Documents.Any())
                {
                    logMsgs.AddRange(scrollResponse.Documents);

                    // Get the next batch using the scroll ID
                    scrollResponse = await _elasticClient.ScrollAsync<LogMessage>("1m", scrollResponse.ScrollId);
                }

                return logMsgs
                    .OrderBy(x => x.TimeStamp)
                    .Select(x => new LogMessage { Level = GetType(x.Level), Message = x.Message, TimeStamp = x.TimeStamp })
                    .ToList();
            }
            catch (Exception ex)
            {
                return new List<LogMessage>();
            }
        }
    }
}
