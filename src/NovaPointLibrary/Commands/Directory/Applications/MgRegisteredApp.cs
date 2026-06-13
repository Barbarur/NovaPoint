using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Context;

namespace NovaPointLibrary.Commands.Directory.Applications;

internal class MgRegisteredApp(IContextManager ctx)
{
    private IContextManager Ctx { get; init; } = ctx;

    internal async Task<IEnumerable<GraphApplication>> GetAllAsync(string optionalQuery = "")
    {
        string endpointPath = $"/applications" + optionalQuery;
        return await new GraphAPIHandler(Ctx.Logger, Ctx.AppClient).GetCollectionAsync<GraphApplication>(endpointPath);
    }

    internal async Task<IEnumerable<GraphUser>> GetOwnersAsync(string appObjectId)
    {
        string endpointPath = $"/applications/{appObjectId}/owners?$select=id,displayName,userPrincipalName";
        return await new GraphAPIHandler(Ctx.Logger, Ctx.AppClient).GetCollectionAsync<GraphUser>(endpointPath);
    }
}
