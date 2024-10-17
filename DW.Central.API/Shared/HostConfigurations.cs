using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DW.Central.API.Shared
{
    internal class HostConfigurations
    {
        protected internal readonly static string KeyVaultUrl = System.Environment.GetEnvironmentVariable("KVPATH", EnvironmentVariableTarget.Process)!;
        protected internal readonly static string AuthorityHost = System.Environment.GetEnvironmentVariable("AUTHORITYHOST", EnvironmentVariableTarget.Process)!;
        protected internal readonly static string TenantId = System.Environment.GetEnvironmentVariable("TENANTID", EnvironmentVariableTarget.Process)!;
        protected internal readonly static string ClientId = System.Environment.GetEnvironmentVariable("CLIENTID", EnvironmentVariableTarget.Process)!;
        protected internal readonly static string CertificateName = System.Environment.GetEnvironmentVariable("CERTIFICATENAME", EnvironmentVariableTarget.Process)!;
        protected internal readonly static string SMTPSVCSecretName = System.Environment.GetEnvironmentVariable("SMTPSVCSECRETNAME", EnvironmentVariableTarget.Process)!;
        protected internal readonly static string SharepointScopeURL = System.Environment.GetEnvironmentVariable("SHAREPOINTSCOPEURL", EnvironmentVariableTarget.Process)!;
        protected internal readonly static string PrivateSite = System.Environment.GetEnvironmentVariable("PRIVATESITE", EnvironmentVariableTarget.Process)!;
        protected internal readonly static string SPTenantSites = System.Environment.GetEnvironmentVariable("SPTENANTSITES", EnvironmentVariableTarget.Process)!;
        protected internal readonly static string SPTenantSubsites = System.Environment.GetEnvironmentVariable("SPTENANTSUBSITES", EnvironmentVariableTarget.Process)!;
        protected internal readonly static string SPSiteLists = System.Environment.GetEnvironmentVariable("SPSITELISTS", EnvironmentVariableTarget.Process)!;
        protected internal readonly static string SPSiteDeduplicateLists = System.Environment.GetEnvironmentVariable("SPSITEDEDUPLICATELISTS", EnvironmentVariableTarget.Process)!;
        protected internal readonly static string SPPagesList = System.Environment.GetEnvironmentVariable("SPPAGESLIST", EnvironmentVariableTarget.Process)!;
        protected internal readonly static string ColumnToSearch = System.Environment.GetEnvironmentVariable("COLUMNTOSEARCH", EnvironmentVariableTarget.Process)!;
        protected internal readonly static string StorageAccount = System.Environment.GetEnvironmentVariable("STORAGEACCOUNT", EnvironmentVariableTarget.Process)!;
        protected internal readonly static string SasCredential = System.Environment.GetEnvironmentVariable("SASCREDENTIAL", EnvironmentVariableTarget.Process)!;
        protected internal readonly static string StorageContainer = System.Environment.GetEnvironmentVariable("STORAGECONTAINER", EnvironmentVariableTarget.Process)!;
        protected internal readonly static string LoginName = System.Environment.GetEnvironmentVariable("LOGINNAME", EnvironmentVariableTarget.Process)!;
    }
}
