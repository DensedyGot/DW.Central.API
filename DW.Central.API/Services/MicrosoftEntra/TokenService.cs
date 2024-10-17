using DW.Central.API.Services.Azure;
using DW.Central.API.Services.Internal;
using DW.Central.API.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DW.Central.API.Services.MicrosoftEntra
{
    public class TokenService
    {
        private KeyvaultService keyvaultService;
        private Type currentClass;
        private StringServices stringService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;
        public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
        {
            keyvaultService = new KeyvaultService();
            currentClass = this.GetType();
            stringService = new StringServices();
            _configuration = configuration;
            _logger = logger;
        }
        public async Task<string> GetTokenFromCertificateAsync(string scopeURL, ILogger logger)
        {
            try
            {
                string[] scopes = new string[] { scopeURL };
                X509Certificate2 certificate = await keyvaultService.GetCertificateAsync(logger);
                IConfidentialClientApplication azureApp = ConfidentialClientApplicationBuilder.Create($"{HostConfigurations.ClientId}")
                        .WithAuthority(new Uri($"https://{HostConfigurations.AuthorityHost}/{HostConfigurations.TenantId}"))
                        .WithCertificate(certificate)
                        .Build();
                AuthenticationResult authPromise = azureApp.AcquireTokenForClient(scopes).ExecuteAsync().GetAwaiter().GetResult();
                return authPromise.AccessToken;
            }
            catch (Exception ex)
            {
                LogErrorService logErrorService = new LogErrorService();
                logErrorService.LogErrorWriteHost(logger, ex);
                throw;
            }
        }

        public string GetTokenFromSecret(string scopeURL, ILogger logger)
        {
            try
            {
                string[] scopes = new string[] { scopeURL };
                string secret = keyvaultService.GetSecret(HostConfigurations.SMTPSVCSecretName, logger);
                IConfidentialClientApplication azureApp = ConfidentialClientApplicationBuilder.Create($"{HostConfigurations.ClientId}")
                    .WithAuthority(new Uri($"https://{HostConfigurations.AuthorityHost}/{HostConfigurations.TenantId}"))
                    .WithClientSecret(secret)
                    .Build();
                AuthenticationResult authPromise = azureApp.AcquireTokenForClient(scopes).ExecuteAsync().GetAwaiter().GetResult();
                return authPromise.AccessToken;
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
