using LogFetcher.Models;
using Microsoft.AspNetCore.Mvc;

namespace LogFetcher.Services.Interface
{
    public interface ILogFetcherService
    {
        Task<bool> CheckUniqueId(string uniqueId);
        Task<List<Email>> GetAlerts(string uniqueId);
        //Task<List<LogMessage>> GetLogs(string uniqueId);
        //Task<List<LogMessage>> SearchLogs(string uniqueId, string searchTerm);
        //Task<List<LogMessage>> SearchLogsBasedOnLevel(string uniqueId, string level);
        //Task<List<LogMessage>> SearchLogsBasedOnTimeRange(string uniqueId, string from, string to);
        Task<Dictionary<string, int>> GetLogsForVisualize(string uniqueId);
        Task<Dictionary<string, int>> GetLogsForVisualizeForLineGraph(string uniqueId);
        Task<Dictionary<string, int>> GetLogsForVisualizeForLineGraphError(string uniqueId);
        Task<List<LogMessage>> SearchLogsTest(string uniqueId, string? searchTerm, string? type, string? fromDate, string? toDate);
    }
}
