using DW.Central.API.Clients;
using DW.Central.API.Services.Azure;
using DW.Central.API.Services.Internal;
using DW.Central.API.Services.MSGraph;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Web;
using Microsoft.Extensions.Configuration;
using DW.Central.API.Services.MicrosoftEntra;

namespace DW.Central.API.Functions
{
    public class FetchSPSitePages
    {
        private readonly ILogger<FetchSPSitePages> _logger;
        private StorageService storageService;
        private MSPages msPagesService;
        private MSWebparts msWebpartsService;
        private JArray infoLogs;
        private JArray errorLogs;
        private Type currentClass;
        private StringServices stringService;
        private IConfiguration configuration;

        public FetchSPSitePages(ILogger<FetchSPSitePages> logger, IConfiguration configuration, TokenService tokenService)
        {
            _logger = logger;
            storageService = new StorageService();
            msPagesService = new MSPages(configuration, tokenService);
            msWebpartsService = new MSWebparts(configuration, tokenService); // Fix: Pass configuration and tokenService
            infoLogs = new JArray();
            errorLogs = new JArray();
            currentClass = this.GetType();
            stringService = new StringServices();
            this.configuration = configuration; // Fix: Assign to the class member
        }

        [Function("FetchSPSitePages")]
        [Description("Fifth Function to run. Requirement is the SPSiteDeduplicateLists.json. See SPSitePages.json for the sample output")]
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

                /*Fetch Selected Lists in Blob Data*/
                JArray blobValueArray = await storageService.GetSPSiteNoDuplicatesListsFromBlobAsync(_logger);
                JToken[] newBlobValueArray = blobValueArray.DistinctBy(x => x["Site"]!.ToString()).ToArray();
                blobValueArray = new JArray(newBlobValueArray);
                siteEndIndex = (siteEndIndex == 0) ? blobValueArray.Count : siteEndIndex;

                /*Start Running Find Process*/
                HttpClient httpClient = await MSGraphClient.GetStaticMSGrapHTTPClientAsync(this.configuration, (ILogger<TokenService>)_logger);
                IConfiguration configuration = new ConfigurationBuilder().Build();
                for (int i = siteStartIndex; i < blobValueArray.Count; i++)
                {
                    JObject siteCollectionObject = (JObject)blobValueArray[i];
                    //if (siteCollectionObject["Site"]!.ToString() != @"https://puma.sharepoint.com/sites/GlobalITInfrastructure") continue;
                    _logger.LogInformation($"Site Collection Index {i} out of {blobValueArray.Count}");
                    if (i >= siteStartIndex && i <= siteEndIndex)
                    {
                        _logger.LogInformation($"Processing Site {siteCollectionObject["Site"]}");
                        _logger.LogInformation($"Processing Time {DateTime.Now.Subtract(startRun).TotalSeconds} seconds");
                        await msPagesService.FetchSitePagesAsync(httpClient, storageService, siteCollectionObject, infoLogs, errorLogs, _logger);
                        /*Write Infomation Logs to Blob*/
                        JObject outputData1 = new JObject();
                        outputData1["info"] = infoLogs;
                        await storageService.ReplaceDataToStorage($"InfoLog_{currentClass.FullName}_{siteStartIndex}_{siteEndText}.json", JsonConvert.SerializeObject(outputData1), _logger);

                        /*Write Infomation Logs to Blob*/
                        outputData1 = new JObject();
                        outputData1["Error"] = errorLogs;
                        await storageService.ReplaceDataToStorage($"ErrorLog_{currentClass.FullName}_{siteStartIndex}_{siteEndText}.json", JsonConvert.SerializeObject(outputData1), _logger);
                    }
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
                await storageService.ReplaceDataToStorage("ErrorLog.json", JsonConvert.SerializeObject(outputData), _logger);

                return new BadRequestObjectResult($"Done Error Running {currentClass.FullName}");
            }
        }
    }
}
