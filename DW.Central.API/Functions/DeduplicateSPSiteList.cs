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
using DW.Central.API.Services.MicrosoftEntra;
using Microsoft.Extensions.Configuration;

namespace DW.Central.API.Functions
{
    public class DeduplicateSPSiteList
    {
        private readonly ILogger<DeduplicateSPSiteList> _logger;
        private readonly IConfiguration _configuration; // Add this line
        private StorageService storageService;
        private MSPages msPagesService;
        private MSWebparts msWebpartsService;
        private JArray infoLogs;
        private JArray errorLogs;
        private Type currentClass;
        private StringServices stringService;

        public DeduplicateSPSiteList(ILogger<DeduplicateSPSiteList> logger, IConfiguration configuration, TokenService tokenService)
        {
            _logger = logger;
            _configuration = configuration; // Add this line
            storageService = new StorageService();
            msPagesService = new MSPages(configuration, tokenService);
            msWebpartsService = new MSWebparts(configuration, tokenService);
            infoLogs = new JArray();
            errorLogs = new JArray();
            currentClass = this.GetType();
            stringService = new StringServices();
        }

        [Function("DeduplicateSPSiteList")]
        [Description("Fourth Function to run. Requirement is the SPSiteLists.json. See SPDeduplicateLists.json for the sample output")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            DateTime startRun = DateTime.Now;
            try
            {
                _logger.LogInformation($"Start Running {currentClass.FullName}");

                /*Parameter Ingestion*/
                Uri reqUri = new Uri(req.GetEncodedUrl());
                string siteStartText = HttpUtility.ParseQueryString(reqUri.Query).Get("sitestart") ?? "0";
                int siteStartIndex = Int32.Parse(siteStartText);

                /*Fetch Selected Lists in Blob Data*/
                JArray blobValueArray = await storageService.GetSPSiteListsFromBlobAsync(_logger);
                JToken[] newBlobValueArray = blobValueArray.DistinctBy(x => x["Site"]!.ToString()).ToArray();
                blobValueArray = new JArray(newBlobValueArray);

                /*Start Running Find Process*/
                HttpClient httpClient = await MSGraphClient.GetStaticMSGrapHTTPClientAsync(_configuration, _logger as ILogger<TokenService>); // Use _configuration
                JArray outputDataArray = new JArray();
                List<string> noDuplicateSite = new List<string>();
                for (int i = siteStartIndex; i < blobValueArray.Count; i++)
                {
                    JObject siteCollectionObject = (JObject)blobValueArray[i];
                    //if (siteCollectionObject["Site"]!.ToString() != @"https://puma.sharepoint.com/sites/GlobalITInfrastructure") continue;
                    _logger.LogInformation($"Site Collection Index {i} out of {blobValueArray.Count}");
                    if (i >= siteStartIndex)
                    {
                        _logger.LogInformation($"Processing Site {siteCollectionObject["Site"]}");
                        _logger.LogInformation($"Processing Time {DateTime.Now.Subtract(startRun).TotalSeconds} seconds");
                        //await msPagesService.FetchSitePagesAsync(httpClient, storageService, siteCollectionObject, infoLogs, errorLogs, _logger);
                        /*Write Infomation Logs to Blob*/
                        if (!noDuplicateSite.Contains(siteCollectionObject["Site"]?.ToString() ?? string.Empty))
                        {
                            outputDataArray.Add(siteCollectionObject);
                            noDuplicateSite.Add(siteCollectionObject["Site"]?.ToString() ?? string.Empty);
                        }
                    }
                }
                JObject outputData1 = new JObject();
                outputData1["info"] = outputDataArray;
                await storageService.ReplaceDataToStorage("InfoLog.json", JsonConvert.SerializeObject(outputData1), _logger);

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
