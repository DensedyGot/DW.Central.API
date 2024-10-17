using DW.Central.API.Services.Dataverse;
using DW.Central.API.Services.MicrosoftEntra;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

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
            _logger.LogInformation("Started FlowMonitoring v4");
            try
            {
                string[]? environmentUrls = _configuration["EnvironmentUrl"]?.Split(',');
                if (environmentUrls == null || environmentUrls.Length <= 1)
                {
                    _logger.LogWarning("Environment URLs are not properly configured 1.");
                    return new BadRequestObjectResult("Environment URLs are not properly configured.");
                }
                string accessToken = await _tokenService.GetTokenFromCertificateAsync($"{environmentUrls[0]}/.default", _logger);
                _logger.LogWarning("Access Token {AccessToken}", accessToken);

                string result = await _checkFlows.CheckFlowRunErrors(accessToken, environmentUrls[0], _logger);

                return new OkObjectResult("Welcome to Azure Functions!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FlowMonitoring: {Message}", ex.Message);
                return new BadRequestObjectResult("Error in FlowMonitoring");
            }
        }
    }
}
