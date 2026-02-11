using Microsoft.SharePoint.Client;
using NovaPointLibrary.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Core.Authentication
{
    public interface IAppClient
    {
        Guid TenantId { get; }
        Guid ClientId { get; }
        string AdminUrl { get; }
        string RootPersonalUrl { get; }
        string RootSharedUrl { get; }
        string Domain { get; set; }

        CancellationToken CancelToken { get; }

        void IsCancelled();
        Task<string> GetGraphAccessToken();
        Task<ClientContext> GetContext(string siteUrl);
        Task<string> GetSPOAccessToken(string siteUrl);
    }
}
