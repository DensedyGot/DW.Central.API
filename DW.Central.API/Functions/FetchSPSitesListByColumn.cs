using DW.Central.API.Services.Azure;
using DW.Central.API.Services.Internal;
using DW.Central.API.Services.Sharepoint;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Web;
using DW.Central.API.Services.MicrosoftEntra;
using Microsoft.Extensions.Configuration;

namespace DW.Central.API.Functions
{
    public class FetchSPSitesListByColumn
    {
        private readonly ILogger<FetchSPSitesListByColumn> _logger;
        private StorageService storageService;
        private SPLists spListsService;
        private JArray infoLogs;
        private JArray errorLogs;
        private Type currentClass;
        private StringServices stringService;

        public FetchSPSitesListByColumn(ILogger<FetchSPSitesListByColumn> logger, IConfiguration configuration, TokenService tokenService)
        {
            _logger = logger;
            storageService = new();
            spListsService = new(configuration, tokenService);
            infoLogs = new();
            errorLogs = new();
            currentClass = this.GetType();
            stringService = new StringServices();
        }

        [Function("FetchSPSitesListByColumn")]
        [Description("Third Function to run. Requirement is the SPTenantSubsites.json. See SPSiteLists.json for the sample output")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            DateTime startRun = DateTime.Now;
            try
            {
                _logger.LogInformation($"Start Running {currentClass.FullName}");

                /*Parameter Ingestion*/
                Uri reqUri = new Uri(req.GetEncodedUrl());
                string siteStartText = HttpUtility.ParseQueryString(reqUri.Query).Get("sitestart") ?? "0";
                string siteEndText = HttpUtility.ParseQueryString(reqUri.Query).Get("siteend") ?? "0";
                int siteStartIndex = Int32.Parse(siteStartText);
                int siteEndIndex = Int32.Parse(siteEndText);

                /*Fetch Selected Sites Collection in Blob Data*/
                JArray blobValueArray = await storageService.GetSPSubsitesFromBlobAsync(_logger);
                siteEndIndex = (siteEndIndex == 0) ? blobValueArray.Count : siteEndIndex;

                /*Start Running Find Process*/
                for (int i = siteStartIndex; i <= siteEndIndex; i++)
                {
                    JObject siteCollectionObject = (JObject)blobValueArray[i];
                    _logger.LogInformation($"Site Collection Index {i} out of {blobValueArray.Count}");

                    _logger.LogInformation($"Processing Site {siteCollectionObject["webUrl"]}");
                    _logger.LogInformation($"Processing Time {DateTime.Now.Subtract(startRun).TotalSeconds} seconds");

                    await spListsService.FetchSiteListAsync(siteCollectionObject, infoLogs, errorLogs, _logger);

                    /*Write Infomation Logs to Blob*/
                    JObject outputData1 = new JObject();
                    outputData1["info"] = infoLogs;
                    await storageService.ReplaceDataToStorage($"InfoLog_{currentClass.FullName}_{siteStartIndex}_{siteEndText}.json", JsonConvert.SerializeObject(outputData1), _logger);

                    /*Write Infomation Logs to Blob*/
                    outputData1 = new JObject();
                    outputData1["Error"] = errorLogs;
                    await storageService.ReplaceDataToStorage($"ErrorLog_{currentClass.FullName}_{siteStartIndex}_{siteEndText}.json", JsonConvert.SerializeObject(outputData1), _logger);

                    /*Write Counter Logs to Blob*/
                    JObject outputData2 = new JObject();
                    outputData2 = new JObject();
                    outputData2["Index"] = i;
                    await storageService.ReplaceDataToStorage($"CounterLog_{currentClass.FullName}_{siteStartIndex}_{siteEndText}.json", JsonConvert.SerializeObject(outputData2), _logger);
                }
                return new OkObjectResult($"Done Running {currentClass.FullName}");
            }
            catch (Exception ex)
            {
                JObject outputDetail = new JObject();
                outputDetail["Path"] = $"{currentClass.FullName}.{stringService.GetOriginalMethodName()}";
                outputDetail["HResult"] = ex.HResult;
                outputDetail["Message"] = ex.Message;
                outputDetail["LineNumber"] = stringService.GetLineNumber(ex); ;
                errorLogs.Add(outputDetail);

                /*Write Error to Blob*/
                JObject outputData = new JObject();
                outputData["error"] = errorLogs;
                await storageService.CreateDataToStorage("ErrorLog_FetchSPSitesEventsList.json", JsonConvert.SerializeObject(outputData), _logger);

                return new BadRequestObjectResult($"Done Error Running {currentClass.FullName}");
            }
        }
    }
}
