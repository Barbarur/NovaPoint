using System.Net;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Context;
using NovaPointLibrary.Core.HttpService;

namespace NovaPointLibrary.Commands.Directory.User;

internal class DirectoryUser(IContextManager ctx)
{
    private IContextManager Ctx { get; init; } = ctx;

    // A user that has been deleted is taken out of '/users' immediately and kept in the
    // directory recycle bin for 30 days before hard deletion, so it does not resolve here
    // whether it is soft-deleted or already gone for good.
    internal async Task<bool> ExistsAsync(string userIdOrUPN)
    {
        string property = Guid.TryParse(userIdOrUPN, out _) ? "id" : "userPrincipalName";
        string endpointPath = $"/users?$select={property}&$filter={property} eq '{userIdOrUPN.Replace("'", "''")}'";

        try
        {
            var collUsers = await new GraphAPIHandler(Ctx.Logger, Ctx.AppClient).GetCollectionAsync<GraphUser>(endpointPath);

            return collUsers.Any();
        }
        catch (HttpRequestFailedException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Graph answers a filter on 'id' as a lookup by key, so a missing user comes back
            // as a 404 instead of an empty collection. Only a filter on another property
            // returns the empty collection this would otherwise rely on.
            Ctx.Logger.Info(GetType().Name, $"User '{userIdOrUPN}' does not exist in the directory");

            return false;
        }
    }
}
