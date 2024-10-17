using DW.Central.API.Clients;
using DW.Central.API.Services.Azure;
using DW.Central.API.Services.Internal;
using DW.Central.API.Services.MicrosoftEntra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DW.Central.API.Services.Sharepoint
{
    internal class SPSubsites
    {
        private SharepointClient spClient;
        private Type currentClass;
        private StringServices stringService;
        internal SPSubsites(IConfiguration configuration, TokenService tokenService)
        {
            spClient = new SharepointClient(configuration, tokenService);
            currentClass = this.GetType();
            stringService = new StringServices();
        }

        internal async Task FetchSubsitesAsync(StorageService storageService, JObject siteObject, JArray infoLogs, JArray errorLogs, ILogger logger)
        {
            try
            {
                /*Write Infomation Logs to Blob*/
                JObject newObject = new();
                newObject["webUrl"] = siteObject["webUrl"];
                newObject["id"] = siteObject["id"];
                newObject["displayName"] = siteObject["displayName"];
                infoLogs.Add(newObject);

                /*Fetch SubSites*/
                ClientContext clientContext = await spClient.GetInstanceSharepointClientAsync(siteObject["webUrl"]!.ToString(), logger);
                JArray httpJSONUrlList = await GetSharepointSubsitesAsync(clientContext, siteObject, infoLogs, errorLogs, logger);
                /*Check Subsites*/
                if (httpJSONUrlList.Count > 0)
                {
                    for (int i = 0; i < httpJSONUrlList.Count; i++)
                    {
                        JObject subsiteObject = (JObject)httpJSONUrlList[i];
                        if (subsiteObject["webUrl"]?.ToString().Substring(0, 13) != "https://puma-" && subsiteObject["webUrl"]?.ToString().Substring(0, 17) != "https://trinomic-")
                        {
                            await FetchSubsitesAsync(storageService, subsiteObject, infoLogs, errorLogs, logger);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // The remote server returned an error: (401) Unauthorized.
                //if (ex.HResult != -2146233079)
                //{
                JObject outputDetail = new();
                outputDetail["Site"] = siteObject["webUrl"];
                outputDetail["Path"] = $"{currentClass.FullName}.{stringService.GetOriginalMethodName()}";
                outputDetail["HResult"] = ex.HResult;
                outputDetail["Message"] = ex.Message;
                var stackTrace = new StackTrace(ex, true);
                var frame = stackTrace.GetFrame(0);
                int? lineNumber = frame?.GetFileLineNumber();
                outputDetail["LineNumber"] = lineNumber;
                errorLogs.Add(outputDetail);
                throw;
                //}
            }
        }


        internal async Task<JArray> GetSharepointSubsitesAsync(ClientContext clientContext, JObject siteObject, JArray infoLogs, JArray errorLogs, ILogger logger)
        {
            try
            {
                WebCollection siteCollection = clientContext.Web.Webs;
                clientContext.Load(siteCollection);
                await clientContext.ExecuteQueryAsync();
                JArray outputListCollection = new();
                for (int i = 0; i < siteCollection.Count; i++)
                {
                    Web web = siteCollection[i];
                    JObject webObject = new();
                    webObject["webUrl"] = web.Url;
                    webObject["id"] = web.Id;
                    webObject["displayName"] = web.Title;
                    outputListCollection.Add(webObject);
                }
                return outputListCollection;
            }
            catch (Exception ex)
            {
                JObject outputDetail = new();
                outputDetail["Site"] = siteObject["webUrl"];
                outputDetail["Path"] = $"{currentClass.FullName}.{stringService.GetOriginalMethodName()}";
                outputDetail["HResult"] = ex.HResult;
                outputDetail["Message"] = ex.Message;
                var stackTrace = new StackTrace(ex, true);
                var frame = stackTrace.GetFrame(0);
                int? lineNumber = frame?.GetFileLineNumber();
                outputDetail["LineNumber"] = lineNumber;
                errorLogs.Add(outputDetail);
                return new JArray();
                throw;
            }
        }
    }
}
