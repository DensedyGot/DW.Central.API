using DW.Central.API.Clients;
using DW.Central.API.Services.Azure;
using DW.Central.API.Services.Internal;
using DW.Central.API.Shared;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DW.Central.API.Services.MicrosoftEntra;
using Microsoft.Extensions.Configuration;

namespace DW.Central.API.Services.MSGraph
{
    internal class MSSites
    {
        private StorageService storageService;
        private Type currentClass;
        private StringServices stringService;
        private readonly IConfiguration configuration;
        private readonly TokenService tokenService;

        internal MSSites(IConfiguration configuration, TokenService tokenService)
        {
            storageService = new();
            currentClass = this.GetType();
            stringService = new StringServices();
            this.configuration = configuration;
            this.tokenService = tokenService;
        }

        internal async Task<JArray> GetTenantAllSitesAsync(JArray infoLogs, JArray errorLogs, ILogger logger)
        {
            try
            {
                HttpClient httpClient = await MSGraphClient.GetStaticMSGrapHTTPClientAsync(configuration, logger as ILogger<TokenService>);
                string httpStringResult = await httpClient.GetStringAsync(Constants.MSGraphGetAllSites);
                JObject httpJSONResult = JObject.Parse(httpStringResult);
                JArray httpJSONValue = (JArray)httpJSONResult["value"]!;
                List<JToken> allItems = httpJSONValue.Select(x => x).ToList();
                List<JToken> httpJSONUrlList = new List<JToken>();
                List<JToken> filteredHttpJSONUrlList = new List<JToken>();
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
                filteredHttpJSONUrlList = httpJSONUrlList.FindAll(x => !x["webUrl"]!.ToString().Contains(HostConfigurations.PrivateSite)).ToList();
                var filteredHttpJSONUrlList1 = filteredHttpJSONUrlList.Select(x => new { webUrl = x["webUrl"], id = x["id"], displayName = x["displayName"]! }).ToArray();
                JArray allSitesArray = JArray.FromObject(filteredHttpJSONUrlList1);
                JObject outputData = new JObject();
                outputData["data"] = allSitesArray;
                infoLogs.Add(outputData);
                return allSitesArray;
            }
            catch (Exception ex)
            {
                JObject outputData = new JObject();
                outputData["info"] = infoLogs;
                await storageService.CreateDataToStorage("Info", JsonConvert.SerializeObject(outputData), logger);

                JObject outputDetail = new JObject();
                outputDetail["Path"] = $"{currentClass.FullName}.{stringService.GetOriginalMethodName()}";
                outputDetail["HResult"] = ex.HResult;
                outputDetail["Message"] = ex.Message;
                outputDetail["LineNumber"] = stringService.GetLineNumber(ex);
                errorLogs.Add(outputDetail);

                /*Write Error to Blob*/
                outputData = new JObject();
                outputData["error"] = errorLogs;
                await storageService.CreateDataToStorage("Error", JsonConvert.SerializeObject(outputData), logger);
                throw;
            }
        }
    }
}
