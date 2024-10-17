using DW.Central.API.Services.Internal;
using DW.Central.API.Services.MicrosoftEntra;
using DW.Central.API.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DW.Central.API.Clients
{
    internal class SharepointClient
    {
        private readonly IConfiguration _configuration;
        private readonly TokenService _tokenService;

        public SharepointClient(IConfiguration configuration, TokenService tokenService)
        {
            _configuration = configuration;
            _tokenService = tokenService;
        }

        internal async Task<ClientContext> GetInstanceSharepointClientAsync(string siteUrl, ILogger logger)
        {
            Type currentClass = typeof(SharepointClient);
            try
            {
                string accessToken = await _tokenService.GetTokenFromCertificateAsync(HostConfigurations.SharepointScopeURL, logger);
                ClientContext context = new ClientContext(siteUrl);
                context.ExecutingWebRequest += (sender, e) =>
                {
                    e.WebRequestExecutor.WebRequest.Headers.Add("Authorization", "Bearer " + accessToken);
                };

                Web web = context.Web;
                context.Load(web, w => w.Title);
                context.ExecuteQuery();
                return context;
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
