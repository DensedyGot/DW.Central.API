using DW.Central.API.Functions;
using DW.Central.API.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Graph;

namespace DW.Central.API.Services.Dataverse
{
    public class CheckFlows
    {

        public async Task<ICheckFlows> CheckFlowRunErrors(string accessToken, IDataverseEnvironment dataverseEnvironment, ILogger _logger)
        {
            // Get the current UTC time and calculate the last 24 hours
            _logger.LogInformation($"FlowMonitoring > CheckFlows.cs > CheckFloRunErrors > Step 1");
            DateTime utcNow = DateTime.UtcNow;
            DateTime last24Hours = utcNow.AddHours(-24);
            string last24HoursIso = last24Hours.ToString("o");
            _logger.LogInformation($"FlowMonitoring > CheckFlows.cs > CheckFloRunErrors > Step 2 > {last24HoursIso}");
            string requestUrl = $"https://api.flow.microsoft.com/providers/Microsoft.ProcessSimple/environments/{dataverseEnvironment.EnvironmentId}/flows/{dataverseEnvironment.FlowId}/runs?startTime={last24HoursIso}";
            _logger.LogInformation($"FlowMonitoring > CheckFlows.cs > CheckFloRunErrors > Step 3 > {requestUrl}");

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            _logger.LogInformation($"FlowMonitoring > CheckFlows.cs > CheckFloRunErrors > Step 4");
            var response = await httpClient.GetAsync(requestUrl);
            _logger.LogInformation($"FlowMonitoring > CheckFlows.cs > CheckFloRunErrors > Step 5");
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to retrieve flow logs. Status Code: {response.StatusCode}, Response: {responseBody}");
                return new ICheckFlows {
                    successCount = 0,
                    failureCount = 0,
                    runningCount = 0,
                    skippedCount = 0
                };
            }

            _logger.LogInformation($"FlowMonitoring > CheckFlows.cs > CheckFloRunErrors > Step 6");
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"FlowMonitoring > CheckFlows.cs > CheckFloRunErrors > Step 7 > {content}");
            JsonDocument logs = JsonDocument.Parse(content);
            _logger.LogInformation($"FlowMonitoring > CheckFlows.cs > CheckFloRunErrors > Step 7.5 > {logs.RootElement.ToString()}");
            JsonElement logsRoot = logs.RootElement;

            // Count successes and failures
            int successCount = 0;
            int failureCount = 0;
            int runningCount = 0;
            int skippedCount = 0;

            if (logsRoot.TryGetProperty("value", out JsonElement valueArray))
            {
                foreach (JsonElement item in valueArray.EnumerateArray())
                {
                    if (item.TryGetProperty("properties", out JsonElement properties))
                    {
                        properties.TryGetProperty("startTime", out JsonElement startTime);
                        _logger.LogInformation($"Start Run Time: {startTime.GetString()}");
                        if (properties.TryGetProperty("status", out JsonElement status))
                        {
                            _logger.LogInformation($"Run Name: {item.GetProperty("name")}, Status: {status.GetString()}");
                            var statuscode = status.GetString() ?? "none";
                            if (statuscode == "Succeeded")
                            {
                                successCount++;
                            }
                            else if (statuscode == "Failed")
                            {
                                failureCount++;
                            }
                            else if (statuscode == "Running")
                            {
                                runningCount++;
                            }
                            else if (statuscode == "Skipped")
                            {
                                skippedCount++;
                            }
                        }
                    }
                }
            }

            var checkFlows = new ICheckFlows
            {
                successCount = successCount,
                failureCount = failureCount,
                runningCount = runningCount,
                skippedCount = skippedCount
            };
            return checkFlows;
        }
    }
}
