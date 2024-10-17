using DW.Central.API.Services.Internal;
using DW.Central.API.Services.MicrosoftEntra;
using DW.Central.API.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DW.Central.API.Clients
{
    internal class MSGraphClient
    {

        private Type currentClass;
        private StringServices stringService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MSGraphClient> _logger;

        internal MSGraphClient(IConfiguration configuration, ILogger<MSGraphClient> logger)
        {
            currentClass = this.GetType();
            stringService = new StringServices();
            _configuration = configuration;
            _logger = logger;
        }

        internal static async Task<HttpClient> GetStaticMSGrapHTTPClientAsync(IConfiguration configuration, ILogger<TokenService> logger)
        {
            try
            {
                TokenService tokenService = new TokenService(configuration, logger);
                string accessToken = await tokenService.GetTokenFromCertificateAsync(Constants.MSGraphScopeURL, logger);
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                Uri graphScopeUri = new Uri(Constants.MSGraphScopeURL);
                httpClient.DefaultRequestHeaders.Add("Host", graphScopeUri.Host);
                return httpClient;
            }
            catch (Exception ex)
            {
                LogErrorService logErrorService = new LogErrorService();
                logErrorService.LogErrorWriteHost(logger, ex);
                throw;
            }
        }

        internal async Task<HttpClient> GetInstanceMSGrapHTTPClientAsync()
        {
            try
            {
                ILogger<TokenService> tokenServiceLogger = _logger as ILogger<TokenService>;
                TokenService tokenService = new TokenService(_configuration, tokenServiceLogger);
                string accessToken = await tokenService.GetTokenFromCertificateAsync(Constants.MSGraphScopeURL, tokenServiceLogger);
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                Uri graphScopeUri = new Uri(Constants.MSGraphScopeURL);
                httpClient.DefaultRequestHeaders.Add("Host", graphScopeUri.Host);
                return httpClient;
            }
            catch (Exception ex)
            {
                LogErrorService logErrorService = new LogErrorService();
                logErrorService.LogErrorWriteHost(_logger, ex);
                throw;
            }
        }
    }
}
