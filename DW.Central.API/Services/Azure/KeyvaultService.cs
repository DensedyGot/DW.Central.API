using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Azure;
using DW.Central.API.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using DW.Central.API.Services.Internal;
using Microsoft.Extensions.Logging;
using System.IO;

namespace DW.Central.API.Services.Azure
{
    internal class KeyvaultService
    {
        private Type currentClass;
        private StringServices stringService;
        internal KeyvaultService()
        {
            currentClass = this.GetType();
            stringService = new StringServices();
        }
        internal async Task<X509Certificate2> GetCertificateAsync(ILogger logger)
        {
            try
            {
                logger.LogInformation($"FlowMonitoring > TokenService.cs = GetTokenFromCertificateAsync > KeyvaultService.cs > GetCertificateAsync > Step 1");
                SecretClient secretClient = new SecretClient(new Uri(HostConfigurations.KeyVaultUrl), new DefaultAzureCredential());
                string kvPath = Environment.GetEnvironmentVariable("KVPATH") ?? throw new ArgumentNullException("KVPATH environment variable is not set.");
                CertificateClient certificateClient = new CertificateClient(vaultUri: new Uri(kvPath), credential: new DefaultAzureCredential());
                logger.LogInformation($"FlowMonitoring > TokenService.cs = GetTokenFromCertificateAsync > KeyvaultService.cs > GetCertificateAsync > Step 2 > {certificateClient.VaultUri}");
                KeyVaultCertificateWithPolicy certificateWithPolicy = await certificateClient.GetCertificateAsync(HostConfigurations.CertificateName);
                logger.LogInformation($"FlowMonitoring > TokenService.cs = GetTokenFromCertificateAsync > KeyvaultService.cs > GetCertificateAsync > Step 3 > {certificateWithPolicy.Name}");
                await Task.Delay(2000);
                KeyVaultSecret secret = await secretClient.GetSecretAsync(certificateWithPolicy.Name);
                logger.LogInformation($"FlowMonitoring > TokenService.cs = GetTokenFromCertificateAsync > KeyvaultService.cs > GetCertificateAsync > Step 4 > {secret.Name}");
                byte[] pfxBytes = Convert.FromBase64String(secret.Value);
                logger.LogInformation($"FlowMonitoring > TokenService.cs = GetTokenFromCertificateAsync > KeyvaultService.cs > GetCertificateAsync > Step 5 > {pfxBytes.Length}");
                X509Certificate2 x509Certificate = new X509Certificate2(pfxBytes, (string?)null, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                logger.LogInformation($"FlowMonitoring > TokenService.cs = GetTokenFromCertificateAsync > KeyvaultService.cs > GetCertificateAsync > Step 6 > {x509Certificate.SubjectName}");
                return x509Certificate;
            }
            catch (Exception ex)
            {
                LogErrorService logErrorService = new LogErrorService();
                logErrorService.LogErrorWriteHost(logger, ex);
                throw;
            }
        }

        internal string GetSecret(string secretName, ILogger logger)
        {
            try
            {
                SecretClient secretClient = new SecretClient(new Uri(HostConfigurations.KeyVaultUrl), new DefaultAzureCredential());
                KeyVaultSecret vaultSecret = secretClient.GetSecret(secretName);
                return vaultSecret.Value;
            }
            catch (Exception ex)
            {
                LogErrorService logErrorService = new LogErrorService();
                logErrorService.LogErrorWriteHost(logger, ex);
                throw;
            }
        }
        public async Task<List<string>> GetCertificateProperties(ILogger logger)
        {
            try
            {
                SecretClient client = new SecretClient(new Uri(HostConfigurations.KeyVaultUrl), new DefaultAzureCredential());
                CertificateClient cerClient = new CertificateClient(vaultUri: new Uri(HostConfigurations.KeyVaultUrl), credential: new DefaultAzureCredential());
                AsyncPageable<CertificateProperties> certProperties = cerClient.GetPropertiesOfCertificatesAsync();
                List<string> expirationDates = new List<string>();
                await foreach (CertificateProperties certProperty in certProperties)
                {
                    if (certProperty.ExpiresOn.HasValue)
                    {
                        DateTimeOffset expirationDate = certProperty.ExpiresOn.Value;
                        DateTimeOffset expirationDateAdjusted = expirationDate.AddDays(-360);
                        DateTimeOffset currentDate = new DateTimeOffset(DateTime.UtcNow);
                        bool isCertExpired = false;
                        if (currentDate >= expirationDateAdjusted)
                        {
                            isCertExpired = true;
                        }
                        string certInfo = $"{certProperty.Name} - {certProperty.ExpiresOn.Value.DateTime.ToString("D")} - {currentDate.DateTime.ToString("D")} - {isCertExpired}";
                        expirationDates.Add(certInfo);
                    }
                }
                return expirationDates;
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
