using DW.Central.API.Functions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DW.Central.API.Services.Dataverse
{
    public class CheckFlows
    {

        public async Task<string> CheckFlowRunErrors(string accessToken, string environmentUrl, ILogger _logger)
        {
            // Get the current UTC time and calculate the last 24 hours
            _logger.LogInformation($"FlowMonitoring > CheckFlows.cs > CheckFloRunErrors > Step 1");
            DateTime utcNow = DateTime.UtcNow;
            DateTime last24Hours = utcNow.AddHours(-24);
            string last24HoursIso = last24Hours.ToString("o");
            _logger.LogInformation($"FlowMonitoring > CheckFlows.cs > CheckFloRunErrors > Step 2");
            // Query for succeeded (statuscode = 1) and failed (statuscode = 2) flows in the last 24 hours
            //string requestUrl = $"{environmentUrl}/api/data/v9.1/systemjobs?$filter=(statuscode eq 1 or statuscode eq 2) and createdon ge {last24HoursIso}";
            string requestUrl = @"https://management.azure.com/providers/Microsoft.ProcessSimple/environments/Default-47ba06f1-76f9-4c6f-b5ad-7cd7af013ffe/flows/dcda08d3-2626-4c97-b207-eca6d676a13b/runs?api-version=2016-11-01";
            _logger.LogInformation($"FlowMonitoring > CheckFlows.cs > CheckFloRunErrors > Step 3 > {requestUrl}");

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            _logger.LogInformation($"FlowMonitoring > CheckFlows.cs > CheckFloRunErrors > Step 4");
            var response = await httpClient.GetAsync(requestUrl);
            _logger.LogInformation($"FlowMonitoring > CheckFlows.cs > CheckFloRunErrors > Step 5");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to retrieve flow logs.");
                return $"Failed to retrieve flow logs. Status code: {response.StatusCode}";
            }
            _logger.LogInformation($"FlowMonitoring > CheckFlows.cs > CheckFloRunErrors > Step 6");
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"FlowMonitoring > CheckFlows.cs > CheckFloRunErrors > Step 7 > {content}");
            var logs = JsonDocument.Parse(content);

            // Count successes and failures
            int successCount = 0;
            int failureCount = 0;

            foreach (var item in logs.RootElement.GetProperty("value").EnumerateArray())
            {
                var statuscode = item.GetProperty("statuscode").GetInt32();
                if (statuscode == 1)
                {
                    successCount++;
                }
                else if (statuscode == 2)
                {
                    failureCount++;
                }
            }
            return $"Success Count: {successCount}, Failure Count: {failureCount}";
        }
    }
}
