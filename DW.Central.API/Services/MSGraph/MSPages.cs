using DW.Central.API.Services.Azure;
using DW.Central.API.Services.Internal;
using DW.Central.API.Services.MicrosoftEntra;
using DW.Central.API.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DW.Central.API.Services.MSGraph
{
    internal class MSPages
    {
        private MSWebparts msWebpartsService;
        private Type currentClass;
        private StringServices stringService;

        internal MSPages(IConfiguration configuration, TokenService tokenService)
        {
            currentClass = GetType();
            msWebpartsService = new MSWebparts(configuration, tokenService);
            stringService = new StringServices();
        }

        internal async Task FetchSitePagesAsync(HttpClient httpClient, StorageService storageService, JObject siteObject, JArray infoLogs, JArray errorLogs, ILogger logger)
        {
            string graphUrl = "";
            try
            {
                graphUrl = Constants.MSGraphGetSitePages.Replace("siteId", siteObject["SiteId"]!.ToString());
                string httpStringResult = await httpClient.GetStringAsync(graphUrl);
                JObject httpJSONResult = JObject.Parse(httpStringResult);
                JArray httpJSONValue = (JArray)httpJSONResult["value"]!;
                List<JToken> allItems = httpJSONValue.Select(x => x).ToList();
                List<JToken> httpJSONUrlList = new List<JToken>();
                httpJSONUrlList.AddRange(allItems);
                if (httpJSONResult[$"@odata.nextLink"] != null)
                {
                    JObject nextHttpJSONResult = httpJSONResult!;
                    JArray nextHttpJSONValue = httpJSONValue;
                    string nextHttpStringResult = "";
                    for (int i = 0; i < Constants.MaxCounterValue; i++)
                    {
                        nextHttpStringResult = await httpClient.GetStringAsync(nextHttpJSONResult[$"@odata.nextLink"]!.ToString());
                        nextHttpJSONResult = JObject.Parse(nextHttpStringResult);
                        nextHttpJSONValue = (JArray)nextHttpJSONResult["value"]!;
                        List<JToken> newHttpJSONUrlList = nextHttpJSONValue.Select(x => x).ToList();
                        httpJSONUrlList.AddRange(newHttpJSONUrlList);
                        if (nextHttpJSONResult["@odata.nextLink"] == null)
                        {
                            break;
                        }
                    }
                }
                /*Check Pages*/
                logger.LogInformation($"{currentClass.FullName}.{stringService.GetOriginalMethodName()} Pages Count {httpJSONUrlList.Count}");
                if (httpJSONUrlList.Count > 0)
                {
                    //List<Task<bool>> batchTask = new();
                    for (int i = 0; i < httpJSONUrlList.Count; i++)
                    {
                        JObject pageObject = (JObject)httpJSONUrlList[i]!;
                        //pageName = pageObject["name"]!.ToString();
                        //    batchTask.Add(FetchWebpartsAsync(httpClient, pageObject, siteObject, infoLogs, errorLogs, logger));
                        //await msWebpartsService.FetchWebpartsAsync(httpClient, pageObject, siteObject, infoLogs, errorLogs, logger);
                        JObject outputDetail = new();
                        outputDetail["Site"] = siteObject["Site"];
                        outputDetail["SiteId"] = siteObject["SiteId"];
                        outputDetail["Page"] = pageObject["webUrl"];
                        outputDetail["PageId"] = pageObject["id"];
                        outputDetail["ListName"] = siteObject["ListName"];
                        infoLogs.Add(outputDetail);
                    }
                    //Task.WaitAll(batchTask.ToArray());
                }
                else
                {
                    JObject outputDetail = new();
                    outputDetail["Site"] = siteObject["Site"];
                    outputDetail["SiteId"] = siteObject["SiteId"];
                    outputDetail["Page"] = string.Empty;
                    outputDetail["PageId"] = string.Empty;
                    outputDetail["ListName"] = siteObject["ListName"];
                    infoLogs.Add(outputDetail);
                }
            }
            catch (Exception ex)
            {
                LogErrorService logErrorService = new LogErrorService();
                logErrorService.LogErrorWriteHost(logger, ex);
                if (ex.HResult == -2146233088)
                {
                    //Response status code does not indicate success: 404 (Not Found).
                }
                else
                {
                    JObject outputDetail = new();
                    outputDetail["Site"] = siteObject["webUrl"];
                    outputDetail["Path"] = $"{currentClass.FullName}.{stringService.GetOriginalMethodName()}";
                    outputDetail["HResult"] = ex.HResult;
                    outputDetail["Message"] = ex.Message;
                    outputDetail["LineNumber"] = stringService.GetLineNumber(ex);
                    errorLogs.Add(outputDetail);
                    throw;
                }
            }
        }

    }
}
