using Newtonsoft.Json;

namespace NovaPointLibrary.Commands.Utilities.GraphModel;

public class GraphDeviceCompliancePolicyState
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    [JsonProperty("displayName")]
    public string DisplayName { get; set; } = string.Empty;
    [JsonProperty("platformType")]
    public string PlatformType { get; set; } = string.Empty;
    [JsonProperty("state")]
    public string State { get; set; } = string.Empty;
    [JsonProperty("version")]
    public int Version { get; set; } = 0;
    [JsonProperty("settingStates")]
    public List<DeviceComplianceSettingState> SettingStates { get; set; } = [];
}

public class DeviceComplianceSettingState
{
    [JsonProperty("setting")]
    public string Setting { get; set; } = string.Empty;
    [JsonProperty("settingName")]
    public string SettingName { get; set; } = string.Empty;
    [JsonProperty("state")]
    public string State { get; set; } = string.Empty;
    [JsonProperty("errorDescription")]
    public string ErrorDescription { get; set; } = string.Empty;
    [JsonProperty("currentValue")]
    public string CurrentValue { get; set; } = string.Empty;
}
