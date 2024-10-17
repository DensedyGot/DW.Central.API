using DW.Central.API.Services.Azure;
using DW.Central.API.Services.Internal;
using DW.Central.API.Services.MSGraph;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.ComponentModel;
using DW.Central.API.Services.MicrosoftEntra;
using Microsoft.Extensions.Configuration;

namespace DW.Central.API.Functions
{
    public class FetchSPTenantSites
    {
        private readonly ILogger<FetchSPTenantSites> _logger;
        private StorageService storageService;
        private MSSites msSitesService;
        private JArray infoLogs;
        private JArray errorLogs;
        private Type currentClass;
        private StringServices stringService;

        public FetchSPTenantSites(ILogger<FetchSPTenantSites> logger, IConfiguration configuration, TokenService tokenService)
        {
            _logger = logger;
            storageService = new StorageService();
            msSitesService = new MSSites(configuration, tokenService);
            infoLogs = new JArray();
            errorLogs = new JArray();
            currentClass = this.GetType();
            stringService = new StringServices();
        }


        [Function("FetchSPTenantSites")]
        [Description("First Function to run. Requirements are the config located in Service Environment. See SPTenantSites.json for the sample output")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation($"Start Running {currentClass.FullName}.{stringService.GetOriginalMethodName()}");
                JArray allSitesList = await msSitesService.GetTenantAllSitesAsync(infoLogs, errorLogs, _logger);
                JArray allSitesArray = JArray.FromObject(allSitesList!);
                /*Write Infomation Logs to Blob*/
                JObject outputData1 = new JObject();
                outputData1["info"] = allSitesArray!;
                outputData1["count"] = allSitesArray.Count;
                await storageService.ReplaceDataToStorage("InfoLog.json", JsonConvert.SerializeObject(outputData1), _logger);
                return new OkObjectResult($"Done Running {currentClass.FullName}.{stringService.GetOriginalMethodName()}");
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
