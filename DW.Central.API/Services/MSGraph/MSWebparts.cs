using DW.Central.API.Clients;
using DW.Central.API.Functions;
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
    internal class MSWebparts
    {
        private SharepointClient spClient;
        private Type currentClass;
        private StringServices stringService;

        internal MSWebparts(IConfiguration configuration, TokenService tokenService)
        {
            spClient = new SharepointClient(configuration, tokenService);
            currentClass = GetType();
            stringService = new StringServices();
        }

        internal async Task FetchWebpartsAsync(HttpClient httpClient, JObject pageObject, JObject siteObject, JArray infoLogs, JArray errorLogs, ILogger logger)
        {
            string webpartName = "";
            string graphUrl = "";
            JObject webpartObject = new JObject();
            try
            {
                /*Fetch Webparts*/
                logger.LogInformation($"{currentClass.FullName}.{stringService.GetOriginalMethodName()} Webpart 1");
                graphUrl = Constants.MSGraphGetPagesWebparts.Replace("siteId", siteObject["SiteId"]!.ToString()).Replace("sitePageId", pageObject["id"]!.ToString());
                logger.LogInformation($"{currentClass.FullName}.{stringService.GetOriginalMethodName()} Webpart 2 {graphUrl}");
                string httpStringResult = await httpClient.GetStringAsync(graphUrl);
                logger.LogInformation($"{currentClass.FullName}.{stringService.GetOriginalMethodName()} Webpart 3 {httpStringResult.Length}");
                JObject httpJSONResult = JObject.Parse(httpStringResult);
                JArray httpJSONValue = (JArray)httpJSONResult["value"]!;
                List<JToken> allItems = httpJSONValue.Select(x => x).ToList();
                List<JToken> httpJSONUrlList = new List<JToken>();
                logger.LogInformation($"{currentClass.FullName}.{stringService.GetOriginalMethodName()} Webpart 4 {httpJSONUrlList.Count}");
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
                logger.LogInformation($"{currentClass.FullName}.{stringService.GetOriginalMethodName()} Webpart 5 {httpJSONUrlList.Count}");
                /*Check Webparts*/
                if (httpJSONUrlList.Count > 0)
                {
                    for (int i = 0; i < httpJSONUrlList.Count; i++)
                    {
                        webpartObject = (JObject)httpJSONUrlList[i];
                        if (webpartObject.ContainsKey("data"))
                        {
                            webpartName = webpartObject["data"]!["title"]!.ToString();

                            if (webpartObject["webPartType"]!.ToString() == Constants.CUSTOMWEBPARTTYPE &&
                                webpartObject["data"]!["title"]!.ToString() == Constants.APPBOOKINGTITLE &&
                                webpartObject["data"]!["description"]!.ToString() == Constants.APPBOOKINGDESCRIPTION)
                            {
                                JObject outputDetail = new();
                                outputDetail["Site"] = siteObject["Site"];
                                outputDetail["Id"] = siteObject["SiteId"];
                                outputDetail["Page"] = pageObject["name"];
                                outputDetail["PageId"] = pageObject["id"];
                                outputDetail["Webpart"] = webpartName;
                                outputDetail["ListName"] = siteObject["ListName"];
                                outputDetail["WebpartId"] = webpartObject["id"]!.ToString();
                                outputDetail["BookingList"] = webpartObject["bookingList"]!.ToString();
                                outputDetail["CalendarList"] = webpartObject["calendarList"]!.ToString();
                                infoLogs.Add(outputDetail);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogErrorService logErrorService = new LogErrorService();
                logErrorService.LogErrorWriteHost(logger, ex);

                if (ex.HResult == -2146233088)
                {
                    //Response status code does not indicate success: 400 (Bad Request).
                }
                else if (ex.HResult == -2147467261)
                {
                    //"Object reference not set to an instance of an object."
                }
                else
                {
                    JObject outputDetail = new();
                    outputDetail["Site"] = siteObject["Site"];
                    outputDetail["Page"] = pageObject["name"];
                    outputDetail["Webpart"] = webpartName;
                    outputDetail["GraphURL"] = graphUrl;
                    outputDetail["SiteObject"] = siteObject;
                    outputDetail["PageObject"] = pageObject;
                    outputDetail["WebpartObject"] = webpartObject;
                    outputDetail["Path"] = $"{currentClass.FullName}.{stringService.GetOriginalMethodName()}";
                    outputDetail["HResult"] = ex.HResult;
                    outputDetail["Message"] = ex.Message;
                    outputDetail["LineNumber"] = stringService.GetLineNumber(ex);
                    errorLogs.Add(outputDetail);
                    throw;
                }
            }
        }

        internal async Task SearchWebpartsAsync(HttpClient httpClient, JObject siteObject, List<SitesWithNoApp> sitesWithNoApps, JArray infoLogs, JArray errorLogs, ILogger logger)
        {
            string webpartName = "";
            string graphUrl = "";
            try
            {
                if (siteObject["PageId"]!.ToString() != string.Empty)
                {
                    graphUrl = Constants.MSGraphGetPagesWebpart.Replace("siteId", siteObject["SiteId"]!.ToString()).Replace("sitePageId", siteObject["PageId"]!.ToString());
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
                    /*Check Webpart*/
                    if (httpJSONUrlList.Count > 0)
                    {
                        for (int i = 0; i < httpJSONUrlList.Count; i++)
                        {
                            JObject webpartObject = (JObject)httpJSONUrlList[i];
                            if (webpartObject["data"] != null)
                            {
                                try
                                {
                                    webpartName = webpartObject["data"]!["title"]!.ToString();
                                }
                                catch (Exception ex)
                                {
                                    LogErrorService logErrorService = new LogErrorService();
                                    logErrorService.LogErrorWriteHost(logger, ex);
                                    webpartName = "";
                                }
                                if (webpartName != "")
                                {
                                    if (webpartObject["webPartType"]!.ToString() == Constants.CUSTOMWEBPARTTYPE &&
                                        webpartObject["data"]!["title"]!.ToString() == Constants.APPBOOKINGTITLE &&
                                        webpartObject["data"]!["description"]!.ToString() == Constants.APPBOOKINGDESCRIPTION)
                                    {
                                        JObject outputDetail = new();
                                        outputDetail["Site"] = siteObject["Site"];
                                        outputDetail["Page"] = siteObject["Page"];
                                        outputDetail["WebpartName"] = webpartObject["data"]!["title"]!.ToString();
                                        outputDetail["WebpartId"] = webpartObject["id"]!.ToString();
                                        try
                                        {
                                            outputDetail["CalendarList"] = webpartObject["data"]!["properties"]!["calendarList"]!.ToString();
                                        }
                                        catch (Exception ex)
                                        {
                                            LogErrorService logErrorService = new LogErrorService();
                                            logErrorService.LogErrorWriteHost(logger, ex);
                                            outputDetail["CalendarList"] = string.Empty;
                                        }
                                        try
                                        {
                                            outputDetail["BookingList"] = webpartObject["data"]!["properties"]!["bookingList"]!.ToString();
                                        }
                                        catch (Exception ex)
                                        {
                                            LogErrorService logErrorService = new LogErrorService();
                                            logErrorService.LogErrorWriteHost(logger, ex);
                                            outputDetail["BookingList"] = string.Empty;
                                        }
                                        infoLogs.Add(outputDetail);

                                        int index = sitesWithNoApps.FindIndex(p => p.SiteURL == siteObject["Site"]?.ToString());
                                        if (index != -1)
                                        {
                                            sitesWithNoApps[index].WithWebpart = true;
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2146233088)
                {
                    LogErrorService logErrorService = new LogErrorService();
                    logErrorService.LogErrorWriteHost(logger, ex);
                }
                else
                {
                    JObject outputDetail = new();
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
