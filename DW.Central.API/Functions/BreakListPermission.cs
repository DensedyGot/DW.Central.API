using DW.Central.API.Clients;
using DW.Central.API.Services.Azure;
using DW.Central.API.Services.Internal;
using DW.Central.API.Services.Sharepoint;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Web;
using DW.Central.API.Services.MicrosoftEntra;
using Microsoft.Extensions.Configuration;

namespace DW.Central.API.Functions
{
    public class BreakListPermission
    {
        private ILogger<BreakListPermission> logger;
        private StorageService storageService;
        private Type currentClass;
        private StringServices stringService;
        private SharepointClient spClient;
        private SPLists spListService;
        private JArray infoLogs;
        private JArray errorLogs;
        private readonly IConfiguration _configuration;
        private readonly TokenService _tokenService;

        public BreakListPermission(ILogger<BreakListPermission> logger, IConfiguration configuration, TokenService tokenService)
        {
            this.logger = logger;
            _configuration = configuration;
            _tokenService = tokenService;
            storageService = new();
            currentClass = this.GetType();
            stringService = new StringServices();
            spClient = new SharepointClient(_configuration, _tokenService);
            spListService = new SPLists(configuration, tokenService);
            infoLogs = new();
            errorLogs = new();
        }

        [Function("BreakListPermission")]
        [Description("Extended Function to run. Requirement is the SPSiteDeduplicateLists.json. See SP")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            spListService = new SPLists(_configuration, _tokenService);
            DateTime startRun = DateTime.Now;
            try
            {
                logger.LogInformation($"Start Running {currentClass.FullName}");

                /*Parameter Ingestion*/
                Uri reqUri = new Uri(req.GetEncodedUrl());
                string siteStartText = HttpUtility.ParseQueryString(reqUri.Query).Get("sitestart") ?? "0";
                logger.LogInformation($"Start Running {currentClass.FullName}");
                string siteEndText = HttpUtility.ParseQueryString(reqUri.Query).Get("siteend") ?? "0";
                int siteStartIndex = Int32.Parse(siteStartText);
                int siteEndIndex = Int32.Parse(siteEndText);

                /*Fetch Selected Lists in Blob Data*/
                JArray blobValueArray = await storageService.GetSPSiteListsFromBlobAsync(logger);
                JToken[] newBlobValueArray = blobValueArray.DistinctBy(x => x["Site"]!.ToString()).ToArray();
                blobValueArray = new JArray(newBlobValueArray);
                siteEndIndex = (siteEndIndex == 0) ? blobValueArray.Count : siteEndIndex;

                /*Start Running Find Process*/
                HttpClient httpClient = await MSGraphClient.GetStaticMSGrapHTTPClientAsync(_configuration, (ILogger<TokenService>)logger);
                for (int i = siteStartIndex; i < blobValueArray.Count; i++)
                {
                    JObject siteListObject = (JObject)blobValueArray[i];
                    logger.LogInformation($"Site List Index {i} out of {blobValueArray.Count}");
                    if (i >= siteStartIndex && i <= siteEndIndex)
                    {
                        logger.LogInformation($"Processing Site {siteListObject["Site"]}/{siteListObject["ListName"]}");
                        logger.LogInformation($"Processing Time {DateTime.Now.Subtract(startRun).TotalSeconds} seconds");
                        ClientContext clientContext = await spClient.GetInstanceSharepointClientAsync(siteListObject["Site"]!.ToString(), logger);
                        await spListService.BreakListPermissionAsync(clientContext, siteListObject, infoLogs, errorLogs, logger);

                        /*Write Infomation Logs to Blob*/
                        JObject outputData1 = new JObject();
                        outputData1["info"] = infoLogs;
                        await storageService.ReplaceDataToStorage($"InfoLog_{currentClass.FullName}_{siteStartIndex}_{siteEndText}.json", JsonConvert.SerializeObject(outputData1), logger);

                        /*Write Infomation Logs to Blob*/
                        outputData1 = new JObject();
                        outputData1["Error"] = errorLogs;
                        await storageService.ReplaceDataToStorage($"ErrorLog_{currentClass.FullName}_{siteStartIndex}_{siteEndText}.json", JsonConvert.SerializeObject(outputData1), logger);
                    }
                    /*Write Counter Logs to Blob*/
                    JObject outputData2 = new JObject();
                    outputData2 = new JObject();
                    outputData2["Index"] = i;
                    await storageService.ReplaceDataToStorage($"CounterLog_{currentClass.FullName}_{siteStartIndex}_{siteEndText}.json", JsonConvert.SerializeObject(outputData2), logger);
                }
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
                await storageService.CreateDataToStorage("ErrorLog.json", JsonConvert.SerializeObject(outputData), logger);

                return new BadRequestObjectResult($"Done Error Running {currentClass.FullName}");
            }
            logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
