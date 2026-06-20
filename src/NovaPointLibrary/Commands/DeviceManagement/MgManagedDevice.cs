using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Context;

namespace NovaPointLibrary.Commands.DeviceManagement;

internal class MgManagedDevice(IContextManager ctx)
{
    private IContextManager Ctx { get; init; } = ctx;

    internal async Task<IEnumerable<GraphManagedDevice>> GetAllAsync(string optionalQuery = "")
    {
        string endpointPath = $"/deviceManagement/managedDevices" + optionalQuery;
        return await new GraphAPIHandler(Ctx.Logger, Ctx.AppClient).GetCollectionAsync<GraphManagedDevice>(endpointPath);
    }

    internal async Task<int> GetCompliancePolicyCountAsync(string deviceId)
    {
        string endpointPath = $"/deviceManagement/managedDevices/{deviceId}/deviceCompliancePolicyStates";
        var policies = await new GraphAPIHandler(Ctx.Logger, Ctx.AppClient)
            .GetCollectionAsync<GraphDeviceCompliancePolicyState>(endpointPath);
        return policies.Count();
    }

    internal async Task<IEnumerable<GraphDeviceCompliancePolicyState>> GetCompliancePolicyStatesAsync(string deviceId)
    {
        string endpointPath = $"/deviceManagement/managedDevices/{deviceId}/deviceCompliancePolicyStates";
        var policies = (await new GraphAPIHandler(Ctx.Logger, Ctx.AppClient)
            .GetCollectionAsync<GraphDeviceCompliancePolicyState>(endpointPath)).ToList();

        foreach (var policy in policies)
        {
            string settingStatesPath = $"/deviceManagement/managedDevices/{deviceId}/deviceCompliancePolicyStates/{policy.Id}/settingStates";
            policy.SettingStates = (await new GraphAPIHandler(Ctx.Logger, Ctx.AppClient)
                .GetCollectionAsync<DeviceComplianceSettingState>(settingStatesPath)).ToList();
        }

        return policies;
    }
}
