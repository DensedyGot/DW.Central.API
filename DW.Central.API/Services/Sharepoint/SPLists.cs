using DW.Central.API.Clients;
using DW.Central.API.Services.Internal;
using DW.Central.API.Services.MicrosoftEntra;
using DW.Central.API.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DW.Central.API.Services.Sharepoint
{
    internal class SPLists
    {
        private SharepointClient spClient;
        private Type currentClass;
        private StringServices stringService;

        internal SPLists(IConfiguration configuration, TokenService tokenService)
        {
            spClient = new SharepointClient(configuration, tokenService);
            currentClass = this.GetType();
            stringService = new StringServices();
        }

        internal async Task FetchSiteListAsync(JObject siteObject, JArray infoLogs, JArray errorLogs, ILogger logger)
        {
            string listName = "";
            try
            {
                /*Fetch List*/
                ClientContext clientContext = await spClient.GetInstanceSharepointClientAsync(siteObject["webUrl"]!.ToString(), logger);
                JArray httpJSONUrlList = await GetSharepointSiteListsAsync(clientContext, siteObject, infoLogs, errorLogs, logger);

                /*Check Lists*/
                if (httpJSONUrlList.Count > 0)
                {
                    for (int i = 0; i < httpJSONUrlList.Count; i++)
                    {
                        int index = i;
                        JObject listObject = (JObject)httpJSONUrlList[index];
                        listName = (string)listObject["name"]!;
                        await GetSharepointListColumnsAsync(clientContext, siteObject, listObject, infoLogs, errorLogs, logger);
                    }
                }
            }
            catch (Exception ex)
            {
                JObject outputDetail = new();
                outputDetail["Site"] = siteObject["webUrl"];
                outputDetail["Path"] = $"{currentClass.FullName}.{stringService.GetOriginalMethodName()}";
                outputDetail["HResult"] = ex.HResult;
                outputDetail["Message"] = ex.Message;
                outputDetail["LineNumber"] = stringService.GetLineNumber(ex);
                errorLogs.Add(outputDetail);
            }
        }

        internal async Task<JArray> GetSharepointSiteListsAsync(ClientContext clientContext, JObject siteObject, JArray infoLogs, JArray errorLogs, ILogger logger)
        {
            try
            {
                ListCollection listCollection = clientContext.Web.Lists;
                clientContext.Load(listCollection);
                await clientContext.ExecuteQueryAsync();
                JArray outputListCollection = new();
                for (int i = 0; i < listCollection.Count; i++)
                {
                    int index = i; // Store the current index in a local variable
                    List list = listCollection[index]; // Use the local index variable
                    JObject listObject = new();
                    listObject["name"] = list.Title;
                    listObject["id"] = list.Id;
                    listObject["created"] = list.Created;
                    outputListCollection.Add(listObject);
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
                outputDetail["LineNumber"] = stringService.GetLineNumber(ex);
                errorLogs.Add(outputDetail);
                return new JArray();
            }
        }

        internal async Task GetSharepointListColumnsAsync(ClientContext clientContext, JObject siteObject, JObject listObject, JArray infoLogs, JArray errorLogs, ILogger logger)
        {
            try
            {
                List list = clientContext.Web.Lists.GetByTitle(listObject["name"]!.ToString());
                FieldCollection fieldCollection = list.Fields;
                clientContext.Load(fieldCollection);
                await clientContext.ExecuteQueryAsync();
                JArray outputColumnCollection = new();
                for (int i = 0; i < fieldCollection.Count; i++)
                {
                    int index = i;
                    Field field = fieldCollection[index];
                    if (field.InternalName == HostConfigurations.ColumnToSearch)
                    {
                        JObject outputDetail = new();
                        outputDetail["Site"] = siteObject["webUrl"];
                        outputDetail["SiteId"] = siteObject["id"];
                        outputDetail["ListName"] = listObject["name"];
                        outputDetail["ListId"] = listObject["id"];
                        outputDetail["Column"] = field.Title;
                        outputDetail["Created"] = listObject["created"];
                        infoLogs.Add(outputDetail);
                    }
                }
            }
            catch (Exception ex)
            {
                //Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.
                if (ex.HResult != -2146233079)
                {
                    JObject outputDetail = new();
                    outputDetail["Site"] = siteObject["webUrl"];
                    outputDetail["Path"] = $"{currentClass.FullName}.{stringService.GetOriginalMethodName()}";
                    outputDetail["HResult"] = ex.HResult;
                    outputDetail["Message"] = ex.Message;
                    outputDetail["FieldName"] = listObject["name"];
                    outputDetail["LineNumber"] = stringService.GetLineNumber(ex);
                    errorLogs.Add(outputDetail);
                }
            }
        }
        internal async Task BreakListPermissionAsync(ClientContext clientContext, JObject listObject, JArray infoLogs, JArray errorLogs, ILogger logger)
        {

            Web web = clientContext.Web;
            clientContext.Load(web);
            clientContext.ExecuteQuery();

            /*Check if List has inherited Solution*/
            List list = web.GetListByTitle(listObject["ListName"]!.ToString());
            clientContext.Load(list, l => l.HasUniqueRoleAssignments);
            clientContext.ExecuteQuery();

            if (!list.HasUniqueRoleAssignments)
            {
                /*Break List Persmission and Hide*/
                list.BreakRoleInheritance(true, false);
                list.Hidden = true;
                list.Update();
                clientContext.ExecuteQuery();

                /*Add User*/
                User newUser = clientContext.Web.EnsureUser(HostConfigurations.LoginName);
                clientContext.Load(newUser);
                clientContext.ExecuteQuery();
                list.AddPermissionLevelToUser(newUser.LoginName, RoleType.Administrator);
                list.Update();
                clientContext.ExecuteQuery();

                JObject outputDetail = new();
                outputDetail["Site"] = listObject["Site"];
                outputDetail["SiteId"] = listObject["SiteId"];
                outputDetail["ListName"] = listObject["ListName"];
                outputDetail["ListId"] = listObject["ListId"];
                outputDetail["Column"] = listObject["Column"];
                infoLogs.Add(outputDetail);
            };

        }
    }
}
