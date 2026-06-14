using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Context;

namespace NovaPointLibrary.Commands.Directory.Applications;

internal class MgServicePrincipalSignInActivity(IContextManager ctx)
{
    private IContextManager Ctx { get; init; } = ctx;

    private const string _betaEndpoint =
        "https://graph.microsoft.com/beta/reports/servicePrincipalSignInActivities";

    internal async Task<Dictionary<string, GraphServicePrincipalSignInActivity>> GetAllAsync()
    {
        var items = await new GraphAPIHandler(Ctx.Logger, Ctx.AppClient)
            .GetCollectionAsync<GraphServicePrincipalSignInActivity>(_betaEndpoint);
        return items
            .Where(a => !string.IsNullOrEmpty(a.AppId))
            .ToDictionary(a => a.AppId, StringComparer.OrdinalIgnoreCase);
    }
}
