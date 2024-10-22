using DW.Central.API.Services.Dataverse;
using DW.Central.API.Services.MicrosoftEntra;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DW.Central.API.Models;
using System.Text.Json;

namespace DW.Central.API.Functions
{
    public class FlowMonitoring
    {
        private readonly ILogger<FlowMonitoring> _logger;
        private readonly CheckFlows _checkFlows;
        private readonly IConfiguration _configuration;
        private readonly TokenService _tokenService;

        public FlowMonitoring(ILogger<FlowMonitoring> logger, IConfiguration configuration, TokenService tokenService)
        {
            _logger = logger;
            _configuration = configuration;
            _tokenService = tokenService;
            _checkFlows = new CheckFlows();
        }

        [Function("FlowMonitoring")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("Started FlowMonitoring v6");
            try
            {
                string? environmentUrl = Environment.GetEnvironmentVariable("EnvironmentUrl");
                _logger.LogInformation("Environment URL: {EnvironmentUrl}", environmentUrl);
                string[]? environmentUrls = environmentUrl?.Split(',');
                if (environmentUrls == null)
                {
                    _logger.LogWarning("Environment URLs are not properly configured.");
                    return new BadRequestObjectResult("Environment URLs are not properly configured.");
                }
                _logger.LogInformation("Environment URL Split: {FirstEnvironmentUrl}", environmentUrls[0]);
                string accessToken = await _tokenService.GetTokenFromCertificateAsync($"https://service.flow.microsoft.com/.default", _logger);
                _logger.LogInformation("Access Token: {AccessToken}", accessToken);
                string environmentId = "Default-47ba06f1-76f9-4c6f-b5ad-7cd7af013ffe";
                string flowId = "dcda08d3-2626-4c97-b207-eca6d676a13b";
                IDataverseEnvironment dataverseEnvironment = new IDataverseEnvironment
                {
                    EnvironmentId = environmentId,
                    FlowId = flowId
                };
                ICheckFlows result = await _checkFlows.CheckFlowRunErrors(accessToken, dataverseEnvironment, _logger);
                _logger.LogInformation("Access Token: {AccessToken}", JsonSerializer.Serialize(result));

                return new OkObjectResult(JsonSerializer.Serialize(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FlowMonitoring: {Message}", ex.Message);
                return new BadRequestObjectResult("Error in FlowMonitoring");
            }
        }
    }
}
