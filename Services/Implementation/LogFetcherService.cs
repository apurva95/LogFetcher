using Elasticsearch.Net;
using LogFetcher.Models;
using LogFetcher.Services.Interface;
using MongoDB.Driver;
using Nest;
using IronPdf;
using System;
using System.Globalization;
using System.Text;
using System.Security.Cryptography.Xml;
using System.Xml;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using System.Net;
using SharpCompress.Common;
using HtmlAgilityPack;

namespace LogFetcher.Services.Implementation
{
    public class LogFetcherService : ILogFetcherService
    {
        private readonly IElasticClient _elasticClient;
        private readonly IMongoDatabase _database;

        public LogFetcherService()
        {
            _elasticClient = CreateElasticClient();
            _database = CreateMongoDbConnection();
        }
        private static IMongoDatabase CreateMongoDbConnection()
        {
            var connectionString = "mongodb+srv://loggerNuget:loggerNuget@loggernugetregistration.3wyibtf.mongodb.net/?retryWrites=true&w=majority";
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            var client = new MongoClient(settings);
            return client.GetDatabase("loggerRegistration");
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

 //       public async Task<List<LogMessage>> GetLogs(string uniqueId)
 //       {
 //           List<LogMessage> logMsgs = new();
 //           var scrollResponse = await _elasticClient.SearchAsync<LogMessage>(s => s
 //                                  .Index(uniqueId)
 //                                  .Size(2) // Set the initial batch size
 //                                  .Scroll("1m") // Set the scroll time window
 //                                  );
 //           while (scrollResponse.Documents.Any())
 //           {
 //               logMsgs.AddRange(scrollResponse.Documents);

 //               // Get the next batch using the scroll ID
 //               scrollResponse = await _elasticClient.ScrollAsync<LogMessage>("1m", scrollResponse.ScrollId);
 //           }
 //           return logMsgs.OrderBy(x => x.TimeStamp).Select(x => new LogMessage { Level = GetType(x.Level), Message = x.Message, TimeStamp = x.TimeStamp }).ToList();
 //       }

 //       public async Task<List<LogMessage>> SearchLogs(string uniqueId, string searchTerm)
 //       {
 //           try
 //           {
 //               var searchResponse = await _elasticClient.SearchAsync<LogMessage>(s => s.Index(uniqueId)
 //                   .Query(q => q.MultiMatch(x => x.Fields(z => z.Field(e => e.Level).Field(e => e.Message)).Query(searchTerm))
 //                   ).Highlight(h => h
 //           .Fields(f => f
 //               .Field(ff => ff.Message).Field(x => x.Level)
 //           )
 //       )
 //               // Add other search parameters and aggregations as needed
 //               );
 //               var logs = searchResponse.Hits.Select(h => h.Source);
 //               return logs.OrderBy(x => x.TimeStamp).Select(x => new LogMessage { Level = GetType(x.Level), Message = x.Message, TimeStamp = x.TimeStamp }).ToList();

 //           }
 //           catch (Exception e)
 //           {
 //               return new();

 //           }
 //       }

 //       public async Task<List<LogMessage>> SearchLogsBasedOnTimeRange(string uniqueId, string from, string to)
 //       {
 //           try
 //           {
 //               List<LogMessage> logMsgs = new();
 //               ISearchResponse<LogMessage> scrollResponse = await _elasticClient.SearchAsync<LogMessage>(s => s
 //    .Index(uniqueId)
 //    .Query(q => q
 //        .DateRange(dr => dr
 //            .Field(f => f.TimeStamp)
 //            .GreaterThanOrEquals(Convert.ToDateTime(from))
 //            .LessThanOrEquals(Convert.ToDateTime(to))
 //        )
 //    )
 //    .Size(2) // Set the initial batch size
 //    .Scroll("1m") // Set the scroll time window
 //);

 //               while (scrollResponse.Documents.Any())
 //               {
 //                   logMsgs.AddRange(scrollResponse.Documents);

 //                   // Get the next batch using the scroll ID
 //                   scrollResponse = await _elasticClient.ScrollAsync<LogMessage>("1m", scrollResponse.ScrollId);
 //               }

 //               return logMsgs
 //                   .OrderBy(x => x.TimeStamp)
 //                   .Select(x => new LogMessage { Level = GetType(x.Level), Message = x.Message, TimeStamp = x.TimeStamp })
 //                   .ToList();
 //           }
 //           catch (Exception ex)
 //           {
 //               return new List<LogMessage>();
 //           }
 //       }

 //       public async Task<List<LogMessage>> SearchLogsBasedOnLevel(string uniqueId, string level)
 //       {
 //           try
 //           {
 //               List<LogMessage> logMsgs = new();
 //               ISearchResponse<LogMessage> scrollResponse;
 //               if (level == "All")
 //               {
 //                   scrollResponse = await _elasticClient.SearchAsync<LogMessage>(s => s
 //                                   .Index(uniqueId)
 //                                   .Size(2) // Set the initial batch size
 //                                   .Scroll("1m") // Set the scroll time window
 //                                   );
 //               }
 //               else
 //               {
 //                   scrollResponse = await _elasticClient.SearchAsync<LogMessage>(s => s.Index(uniqueId)
 //           .Query(q => q
 //               .Terms(t => t
 //                   .Field(f => f.Level)
 //                   .Terms(new List<string> { level.ToLower() })
 //               )
 //           ).Size(2) // Set the initial batch size
 //                                       .Scroll("1m") // Set the scroll time window
 //       );
 //               }
 //               while (scrollResponse.Documents.Any())
 //               {
 //                   logMsgs.AddRange(scrollResponse.Documents);

 //                   // Get the next batch using the scroll ID
 //                   scrollResponse = await _elasticClient.ScrollAsync<LogMessage>("1m", scrollResponse.ScrollId);
 //               }
 //               // Perform the search request
 //               //var searchResponse1 = await _elasticClient.SearchAsync<LogMessage>(searchDescriptor);

 //               // Process the search results and return them
 //               //var logs = searchResponse.Hits.Select(hit => hit.Source);
 //               return logMsgs.OrderBy(x => x.TimeStamp).Select(x => new LogMessage { Level = GetType(x.Level), Message = x.Message, TimeStamp = x.TimeStamp, CallingFile = x.CallingFile, CallingMethod = x.CallingMethod }).ToList();
 //           }
 //           catch (Exception ex)
 //           {
 //               return new();
 //           }
 //       }

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
                //var level = ExtractLogLevel(logMessage.Message);
                if (!dict.ContainsKey(logMessage.Level))
                {
                    dict.Add(logMessage.Level, 1);
                }
                else
                {
                    dict[logMessage.Level]++;
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
                if (logMessages.Any())
                {
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
                if (logMessages.Any())
                {
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
                }
                return dict;
            }
            catch (Exception e)
            {
                return new();
            }
        }

        public async Task<List<LogMessage>> SearchLogsTest(string uniqueId, string? searchTerm, string type, string? fromDate, string? toDate)
        {
            try
            {

                var boolQuery = new BoolQuery();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    boolQuery.Must ??= new List<QueryContainer>();
                    boolQuery.Must = boolQuery.Must.Append(new QueryStringQuery
                    {
                        Fields = new[] { "level", "message", "timestamp", "callingMethod", "callingFile" },
                        Query = $"*{searchTerm}*",
                        Fuzziness = Fuzziness.Auto
                    }).ToList();
                }

                if (!string.IsNullOrEmpty(type) && type != "All")
                {
                    boolQuery.Must = boolQuery.Must ?? new List<QueryContainer>();
                    boolQuery.Must = boolQuery.Must.Append(new TermsQuery
                    {
                        Field = "level",
                        Terms = new List<string> { type.ToLower() }
                    }).ToList();
                }

                if (!string.IsNullOrEmpty(fromDate) || !string.IsNullOrEmpty(toDate))
                {
                    boolQuery.Must = boolQuery.Must ?? new List<QueryContainer>();
                    boolQuery.Must = boolQuery.Must.Append(new DateRangeQuery
                    {
                        Field = "timeStamp",
                        GreaterThanOrEqualTo = Convert.ToDateTime(fromDate),
                        LessThanOrEqualTo = Convert.ToDateTime(toDate)
                    }).ToList();
                }

                var searchDescriptor = new SearchDescriptor<LogMessage>()
    .Index(uniqueId)
    .Query(q => q.Bool(b => boolQuery))
    .Size(10000) // Set the initial batch size
    .Scroll("1m") // Keep the search context alive for 1 minute
    .Highlight(h => h.Fields(f => f.Field(ff => ff.Message).Field(ff => ff.Level)));

                var searchResponse = await _elasticClient.SearchAsync<LogMessage>(searchDescriptor);

                List<LogMessage> logMsgs = new();
                logMsgs.AddRange(searchResponse.Documents);

                while (searchResponse.Documents.Any())
                {
                    // Get the next batch using the scroll ID
                    searchResponse = await _elasticClient.ScrollAsync<LogMessage>("1m", searchResponse.ScrollId);
                    logMsgs.AddRange(searchResponse.Documents);
                }

                //var logs = searchResponse.Hits.Select(h => h.Source);
                return logMsgs.OrderBy(x => x.TimeStamp).Select(x => new LogMessage
                {
                    Level = GetType(x.Level),
                    Message = x.Message,
                    TimeStamp = x.TimeStamp,
                    CallingFile = x.CallingFile,
                    CallingMethod = x.CallingMethod,
                    LevelForDoc = x.Level,
                    MessageForDoc = x.MessageForDoc
                }).ToList();
            }
            catch (Exception e)
            {
                throw new Exception("An error occurred during log search.", e);
            }
        }

        public async Task<List<Email>> GetAlerts(string uniqueId)
        {
            try
            {
                var collection = _database.GetCollection<Registration>("registrations");
                var data = await collection.FindAsync(x => x.RegistrationId == uniqueId);
                if (data != null)
                {
                    return data.First().Emails;
                }
            }
            catch(Exception e)
            {

            }
            return new();
        }

        public async Task GenerateDoc(string uniqueID, string? searchTerm, string type, string? fromDate, string? toDate, string docType)
        {
            var logMessages = await SearchLogsTest(uniqueID, searchTerm, type, fromDate, toDate);
            // Create HTML content for A4 size paper
            string htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        @page {{
            size: landscape;
        }}
        body {{
            font-family: Arial, sans-serif;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            table-layout: auto;
        }}
        th, td {{
            border: 1px solid #ddd;
            padding: 8px;
            word-wrap: break-word;
        }}
        thead {{
            display: table-header-group;
        }}
        .ironpdf-watermark-container,
        .ironpdf-link-container,
        .ironpdf-link {{
            display: none !important;
        }}
        thead th {{
            position: sticky;
            top: 0;
            background-color: #f5f5f5;
        }}
    </style>
</head>
<body>
    <table>
        <thead>
            <tr>
                <th>Timestamp</th>
                <th>Level</th>
                <th>Message</th>
                <th>Calling File</th>
                <th>Calling Method</th>
            </tr>
        </thead>
        <tbody>";

            foreach (var logMessage in logMessages)
            {
                htmlContent += $@"
    <tr>
        <td>{logMessage.TimeStamp}</td>
        <td>{logMessage.LevelForDoc}</td>
        <td>{logMessage.MessageForDoc}</td>
        <td>{logMessage.CallingFile}</td>
        <td>{logMessage.CallingMethod}</td>
    </tr>";
            }

            htmlContent += @"
        </tbody>
    </table>
</body>
</html>";
            // Upload PDF to S3 bucket
            string s3BucketName = "loggerfiles";
            // Create an instance of AmazonS3Client with access key and secret key
            var credentials = new Amazon.Runtime.BasicAWSCredentials("AKIA5XCKO26F6MWVHWDB", "TToHewimkZMIiuVoPxD8RguDA3vZun2djn2qSjj7");
            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.EUWest2
            };
            switch (docType)
            {
                case "pdf":
                    var renderer = new HtmlToPdf();
                    using (var pdf = renderer.RenderHtmlAsPdf(htmlContent))
                    {
                        // Save PDF to a file
                        string fileName = uniqueID + ".pdf";
                        pdf.SaveAs(fileName);
                        using (var client = new AmazonS3Client(credentials, config))
                        {
                            var bucketExists = await client.ListBucketsAsync();
                            using (var fileStream = new FileStream(fileName, FileMode.Open))
                            {
                                var uploadRequest = new TransferUtilityUploadRequest()
                                {
                                    //FilePath = fileName,
                                    InputStream = fileStream,
                                    Key = $"logFiles/{fileName}",
                                    BucketName = s3BucketName,
                                    CannedACL = S3CannedACL.PublicRead,
                                    StorageClass = S3StorageClass.StandardInfrequentAccess
                                };
                                var transferUtility = new TransferUtility(client);
                                await transferUtility.UploadAsync(uploadRequest);
                            }
                        }
                        File.Delete(fileName);
                    }
                    break;
                case "csv":
                    StringBuilder csvContent = new StringBuilder();
                    csvContent.AppendLine("Timestamp,Level,Message,Calling File,Calling Method");

                    // Load the HTML content into HtmlDocument
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(htmlContent);

                    // Extract table rows from the HTML
                    var tableRows = htmlDocument.DocumentNode.SelectNodes("//table/tbody/tr");

                    // Iterate over the table rows and extract the data
                    foreach (var row in tableRows)
                    {
                        var cells = row.Descendants("td").Select(cell => cell.InnerText.Trim());
                        csvContent.AppendLine(string.Join(",", cells));
                    }

                    string csvFileName = uniqueID + ".csv";
                    File.WriteAllText(csvFileName, csvContent.ToString());

                    using (var client = new AmazonS3Client(credentials, config))
                    {
                        using (var fileStream = new FileStream(csvFileName, FileMode.Open))
                        {
                            var uploadRequest = new TransferUtilityUploadRequest()
                            {
                                InputStream = fileStream,
                                Key = $"logFiles/{csvFileName}",
                                BucketName = s3BucketName,
                                CannedACL = S3CannedACL.PublicRead,
                                StorageClass = S3StorageClass.StandardInfrequentAccess
                            };
                            var transferUtility = new TransferUtility(client);
                            await transferUtility.UploadAsync(uploadRequest);
                        }
                    }

                    File.Delete(csvFileName);
                    break;
                default:
                    break;
            }
        }
    }
}
