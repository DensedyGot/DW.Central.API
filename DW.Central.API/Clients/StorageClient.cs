using Azure.Storage.Blobs;
using Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DW.Central.API.Shared;
using DW.Central.API.Services.Internal;

namespace DW.Central.API.Clients
{
    internal class StorageClient
    {
        internal StorageClient()
        {

        }
        internal static BlobServiceClient GetStaticBlobServiceClient(ILogger logger)
        {
            Type currentClass = typeof(StorageClient);
            BlobServiceClient blobServiceClient;
            try
            {
                Uri storageUri = new(HostConfigurations.StorageAccount);
                AzureSasCredential sasCredential = new(HostConfigurations.SasCredential.ToString());
                blobServiceClient = new(storageUri, sasCredential);
                return blobServiceClient;
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
