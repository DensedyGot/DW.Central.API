using Azure.Storage.Blobs;
using DW.Central.API.Clients;
using DW.Central.API.Services.Internal;
using DW.Central.API.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DW.Central.API.Services.Azure
{
    internal class StorageService
    {
        private Type currentClass;
        private StringServices stringService;
        internal StorageService()
        {
            currentClass = this.GetType();
            stringService = new StringServices();
        }
        internal async Task<JArray> GetSPSitesFromBlobAsync(ILogger logger)
        {
            try
            {
                HttpClient blobHttpClient = new HttpClient();
                string blobHttpStringResult = await blobHttpClient.GetStringAsync($"{HostConfigurations.SPTenantSites}{HostConfigurations.SasCredential}");
                JObject blobHttpJSONResult = JObject.Parse(blobHttpStringResult);
                JArray blobHttpJSONValue = (JArray)blobHttpJSONResult["info"]!;
                return blobHttpJSONValue;
            }
            catch (Exception ex)
            {
                LogErrorService logErrorService = new LogErrorService();
                logErrorService.LogErrorWriteHost(logger, ex);
                throw;
            }
        }

        internal async Task<JArray> GetSPSubsitesFromBlobAsync(ILogger logger)
        {
            try
            {
                HttpClient blobHttpClient = new HttpClient();
                string blobHttpStringResult = await blobHttpClient.GetStringAsync($"{HostConfigurations.SPTenantSubsites}{HostConfigurations.SasCredential}");
                JObject blobHttpJSONResult = JObject.Parse(blobHttpStringResult);
                JArray blobHttpJSONValue = (JArray)blobHttpJSONResult["info"]!;
                return blobHttpJSONValue;
            }
            catch (Exception ex)
            {
                LogErrorService logErrorService = new LogErrorService();
                logErrorService.LogErrorWriteHost(logger, ex);
                throw;
            }
        }

        internal async Task<JArray> GetSPSiteListsFromBlobAsync(ILogger logger)
        {
            try
            {
                HttpClient blobHttpClient = new HttpClient();
                string blobHttpStringResult = await blobHttpClient.GetStringAsync($"{HostConfigurations.SPSiteLists}{HostConfigurations.SasCredential}");
                JObject blobHttpJSONResult = JObject.Parse(blobHttpStringResult);
                JArray blobHttpJSONValue = (JArray)blobHttpJSONResult["info"]!;
                return blobHttpJSONValue;
            }
            catch (Exception ex)
            {
                LogErrorService logErrorService = new LogErrorService();
                logErrorService.LogErrorWriteHost(logger, ex);
                throw;
            }
        }

        internal async Task<JArray> GetSPSiteNoDuplicatesListsFromBlobAsync(ILogger logger)
        {
            try
            {
                HttpClient blobHttpClient = new HttpClient();
                string blobHttpStringResult = await blobHttpClient.GetStringAsync($"{HostConfigurations.SPSiteDeduplicateLists}{HostConfigurations.SasCredential}");
                JObject blobHttpJSONResult = JObject.Parse(blobHttpStringResult);
                JArray blobHttpJSONValue = (JArray)blobHttpJSONResult["info"]!;
                return blobHttpJSONValue;
            }
            catch (Exception ex)
            {
                LogErrorService logErrorService = new LogErrorService();
                logErrorService.LogErrorWriteHost(logger, ex);
                throw;
            }
        }

        internal async Task<JArray> GetSPSitePagesFromBlobAsync(ILogger logger)
        {
            try
            {
                HttpClient blobHttpClient = new HttpClient();
                string blobHttpStringResult = await blobHttpClient.GetStringAsync($"{HostConfigurations.SPPagesList}{HostConfigurations.SasCredential}");
                JObject blobHttpJSONResult = JObject.Parse(blobHttpStringResult);
                JArray blobHttpJSONValue = (JArray)blobHttpJSONResult["info"]!;
                return blobHttpJSONValue;
            }
            catch (Exception ex)
            {
                LogErrorService logErrorService = new LogErrorService();
                logErrorService.LogErrorWriteHost(logger, ex);
                throw;
            }
        }

        internal async Task CreateDataToStorage(string filename, string content, ILogger logger)
        {
            try
            {
                BlobServiceClient blobServiceClient = StorageClient.GetStaticBlobServiceClient(logger)!;
                BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(HostConfigurations.StorageContainer);
                MemoryStream mStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
                string dateString = DateTime.Now.ToString("yyyyMMddhhmmssfff");
                await blobContainerClient.UploadBlobAsync($"{filename}_{dateString}.json", mStream);
            }
            catch (Exception ex)
            {
                LogErrorService logErrorService = new LogErrorService();
                logErrorService.LogErrorWriteHost(logger, ex);
                throw;
            }
        }

        internal async Task ReplaceDataToStorage(string filename, string content, ILogger logger)
        {
            try
            {
                BlobServiceClient blobServiceClient = StorageClient.GetStaticBlobServiceClient(logger)!;
                BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(HostConfigurations.StorageContainer);
                MemoryStream mStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
                BlobClient blobClient = blobContainerClient.GetBlobClient(filename);
                if (blobClient != null)
                {
                    blobClient.Upload(mStream, true);
                }
                else
                {
                    await blobContainerClient.UploadBlobAsync($"{filename}.json", mStream);
                }
            }
            catch (Exception ex)
            {
                LogErrorService logErrorService = new LogErrorService();
                logErrorService.LogErrorWriteHost(logger, ex);
                throw;
            }
        }
    }
}
