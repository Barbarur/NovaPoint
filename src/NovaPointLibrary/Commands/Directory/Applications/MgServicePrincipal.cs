using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Authentication;
using NovaPointLibrary.Core.Logging;

namespace NovaPointLibrary.Commands.Directory.Applications;

internal class MgServicePrincipal
{
    private readonly ILogger _logger;
    private readonly IAppClient _appInfo;

    internal MgServicePrincipal(ILogger logger, IAppClient appInfo)
    {
        _logger = logger;
        _appInfo = appInfo;
    }

    internal async Task<IEnumerable<GraphServicePrincipal>> GetAllAsync(string optionalQuery = "")
    {
        string endpointPath = "/servicePrincipals" + optionalQuery;
        return await new GraphAPIHandler(_logger, _appInfo).GetCollectionAsync<GraphServicePrincipal>(endpointPath);
    }

    internal async Task<GraphServicePrincipal> GetByIdAsync(Guid spId, string optionalQuery = "")
    {
        string endpointPath = $"/servicePrincipals/{spId}" + optionalQuery;
        return await new GraphAPIHandler(_logger, _appInfo).GetObjectAsync<GraphServicePrincipal>(endpointPath);
    }

    internal async Task<IEnumerable<GraphUser>> GetOwnersAsync(Guid spId)
    {
        string endpointPath = $"/servicePrincipals/{spId}/owners?$select=id,displayName,userPrincipalName,mail";
        return await new GraphAPIHandler(_logger, _appInfo).GetCollectionAsync<GraphUser>(endpointPath);
    }

    internal async Task<IEnumerable<GraphOAuth2PermissionGrant>> GetOAuth2PermissionGrantsAsync(Guid spId)
    {
        string endpointPath = $"/servicePrincipals/{spId}/oauth2PermissionGrants";
        return await new GraphAPIHandler(_logger, _appInfo).GetCollectionAsync<GraphOAuth2PermissionGrant>(endpointPath);
    }

    internal async Task<IEnumerable<GraphAppRoleAssignment>> GetAppRoleAssignmentsAsync(Guid spId)
    {
        string endpointPath = $"/servicePrincipals/{spId}/appRoleAssignments";
        return await new GraphAPIHandler(_logger, _appInfo).GetCollectionAsync<GraphAppRoleAssignment>(endpointPath);
    }
}
