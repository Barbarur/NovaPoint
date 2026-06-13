using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Context;

namespace NovaPointLibrary.Commands.Directory.Applications;

internal class MgServicePrincipal(IContextManager ctx)
{
    private IContextManager Ctx { get; init; } = ctx;

    internal async Task<IEnumerable<GraphServicePrincipal>> GetAllAsync(string optionalQuery = "")
    {
        string endpointPath = "/servicePrincipals" + optionalQuery;
        return await new GraphAPIHandler(Ctx.Logger, Ctx.AppClient).GetCollectionAsync<GraphServicePrincipal>(endpointPath);
    }

    internal async Task<GraphServicePrincipal> GetByIdAsync(Guid spId, string optionalQuery = "")
    {
        string endpointPath = $"/servicePrincipals/{spId}" + optionalQuery;
        return await new GraphAPIHandler(Ctx.Logger, Ctx.AppClient).GetObjectAsync<GraphServicePrincipal>(endpointPath);
    }

    internal async Task<GraphServicePrincipal?> GetByAppIdAsync(string appId, string optionalQuery = "")
    {
        string endpointPath = $"/servicePrincipals?$filter=appId eq '{appId}'" + optionalQuery;
        var results = await new GraphAPIHandler(Ctx.Logger, Ctx.AppClient).GetCollectionAsync<GraphServicePrincipal>(endpointPath);
        return results.FirstOrDefault();
    }

    internal async Task<IEnumerable<GraphUser>> GetOwnersAsync(Guid spId)
    {
        string endpointPath = $"/servicePrincipals/{spId}/owners?$select=id,displayName,userPrincipalName,mail";
        return await new GraphAPIHandler(Ctx.Logger, Ctx.AppClient).GetCollectionAsync<GraphUser>(endpointPath);
    }

    internal async Task<IEnumerable<GraphOAuth2PermissionGrant>> GetOAuth2PermissionGrantsAsync(Guid spId)
    {
        string endpointPath = $"/servicePrincipals/{spId}/oauth2PermissionGrants";
        return await new GraphAPIHandler(Ctx.Logger, Ctx.AppClient).GetCollectionAsync<GraphOAuth2PermissionGrant>(endpointPath);
    }

    internal async Task<IEnumerable<GraphAppRoleAssignment>> GetAppRoleAssignmentsAsync(Guid spId)
    {
        string endpointPath = $"/servicePrincipals/{spId}/appRoleAssignments";
        return await new GraphAPIHandler(Ctx.Logger, Ctx.AppClient).GetCollectionAsync<GraphAppRoleAssignment>(endpointPath);
    }
}
