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
using System.Diagnostics;
using System.Web;
using Microsoft.Extensions.Configuration;
using DW.Central.API.Services.MicrosoftEntra;

namespace DW.Central.API.Functions
{
    public class SitesWithNoApp
    {
        public string? SiteURL { get; set; }
        public bool WithWebpart { get; set; }
    }
    public class FetchSPSitesWebpart
    {
        private readonly ILogger<FetchSPSitesWebpart> _logger;
        private StorageService storageService;
        private MSWebparts msWebpartService;
        private JArray infoLogs;
        private JArray errorLogs;
        private Type currentClass;
        private StringServices stringService;
        private IConfiguration configuration;

        public FetchSPSitesWebpart(ILogger<FetchSPSitesWebpart> logger, IConfiguration configuration, TokenService tokenService)
        {
            _logger = logger;
            storageService = new();
            msWebpartService = new(configuration, tokenService);
            infoLogs = new();
            errorLogs = new();
            currentClass = this.GetType();
            stringService = new StringServices();
            this.configuration = configuration;
        }

        [Function("FetchSPSitesWebpart")]
        [Description("Last Function to run. Requirement is the SPSitePages.json. See SPSiteWebpart.json for the sample output")]
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

                /*Fetch All Site Collection in Blob Data*/
                //JArray blobValueArray = await storageService.GetFilteredSPSitesFromBlobAsync(logger);
                JArray blobValueArray = await storageService.GetSPSitePagesFromBlobAsync(_logger);
                siteEndIndex = (siteEndIndex == 0) ? blobValueArray.Count : siteEndIndex;

                /*Start Running Find Process*/
                HttpClient httpClient = await MSGraphClient.GetStaticMSGrapHTTPClientAsync(this.configuration, _logger as ILogger<TokenService>);
                List<SitesWithNoApp> sitesWithNoApp = new();
                for (int i = siteStartIndex; i < blobValueArray.Count; i++)
                {
                    JObject sitePageObject = (JObject)blobValueArray[i];
                    IConfiguration configuration = new ConfigurationBuilder().Build();
                    HttpClient localHttpClient = await MSGraphClient.GetStaticMSGrapHTTPClientAsync(configuration, _logger as ILogger<TokenService> ?? throw new ArgumentNullException(nameof(_logger)));
                    //if (sitePageObject["Site"]!.ToString() != @"https://puma.sharepoint.com/sites/200027") continue;
                    _logger.LogInformation($"Site Page Index {i} out of {blobValueArray.Count}");
                    if (i >= siteStartIndex && i <= siteEndIndex)
                    {
                        if (sitePageObject["Site"]!.ToString() != @"https://puma.sharepoint.com/sites/300900")
                        {
                            _logger.LogInformation($"Processing Site {sitePageObject["Site"]}/{sitePageObject["Page"]}");
                            _logger.LogInformation($"Processing Time {DateTime.Now.Subtract(startRun).TotalSeconds} seconds");

                            //await spJSONDataService.SearchSitePageAsync(httpClient, sitePageObject, infoLogs, errorLogs, subsiteStartIndex, logger);
                            bool hasSite = sitesWithNoApp.Any(p => p.SiteURL == sitePageObject["Site"]?.ToString());
                            if (!hasSite)
                            {
                                sitesWithNoApp.Add(new SitesWithNoApp
                                {
                                    SiteURL = sitePageObject["Site"]?.ToString() ?? string.Empty,
                                    WithWebpart = false
                                });
                            }
                            await msWebpartService.SearchWebpartsAsync(localHttpClient, sitePageObject, sitesWithNoApp, infoLogs, errorLogs, _logger);
                            /*Write Infomation Logs to Blob*/
                            JObject outputData1 = new JObject();
                            outputData1["info"] = infoLogs;
                            await storageService.ReplaceDataToStorage($"InfoLog_{currentClass.FullName}_{siteStartIndex}_{siteEndText}.json", JsonConvert.SerializeObject(outputData1), _logger);

                            /*Write Infomation Logs to Blob*/
                            outputData1 = new JObject();
                            outputData1["Error"] = errorLogs;
                            await storageService.ReplaceDataToStorage($"ErrorLog_{currentClass.FullName}_{siteStartIndex}_{siteEndText}.json", JsonConvert.SerializeObject(outputData1), _logger);

                            /*Write Infomation Orphan List*/
                            outputData1["Info"] = JToken.FromObject(sitesWithNoApp);
                            await storageService.ReplaceDataToStorage($"OrphanLog_{currentClass.FullName}_{siteStartIndex}_{siteEndText}.json", JsonConvert.SerializeObject(outputData1), _logger);
                        }
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
                var stackTrace = new StackTrace(ex, true);
                var frame = stackTrace.GetFrame(0);
                int? lineNumber = frame?.GetFileLineNumber();
                outputDetail["LineNumber"] = lineNumber;
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
