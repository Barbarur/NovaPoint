using Newtonsoft.Json;

namespace NovaPointLibrary.Commands.Utilities.GraphModel;

public class GraphVerifiedPublisher
{
    [JsonProperty("displayName")]
    public string? DisplayName { get; set; }
    [JsonProperty("verifiedPublisherId")]
    public object? VerifiedPublisherId { get; set; }
    [JsonProperty("addedDateTime")]
    public object? AddedDateTime { get; set; }
}