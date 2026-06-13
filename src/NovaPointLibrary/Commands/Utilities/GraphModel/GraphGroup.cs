using Newtonsoft.Json;

namespace NovaPointLibrary.Commands.Utilities.GraphModel
{
    internal class GraphGroup
    {
        [JsonProperty("@odata.context")]
        public string? Context { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }  = string.Empty;

        [JsonProperty("createdDateTime")]
        public DateTime CreatedDateTime { get; set; }

        [JsonProperty("mail")]
        public string Email { get; set; } = string.Empty;

        [JsonProperty("groupTypes")]
        public List<string> GroupTypes { get; set; }

        [JsonProperty("mailEnabled")]
        public bool MailEnabled { get; set; }

        [JsonProperty("securityEnabled")]
        public bool SecurityEnabled { get; set; }

        [JsonProperty("visibility")]
        public string Visibility { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        public bool IsMs365Group { get; set; } = false;
        public bool IsEmailEnabledSecurityGroup { get; set; } = false;
        public bool IsSecurityGroup { get; set; } = false;
        public bool IsDistributionList { get; set; } = false;

        internal void DefineTypeGroup()
        {
            if (this.GroupTypes != null && this.GroupTypes.Contains("Unified")) { this.IsMs365Group = true; }
            else if (this.SecurityEnabled)
            {
                if (this.MailEnabled)
                {
                    this.IsEmailEnabledSecurityGroup = true;
                }
                else
                {
                    this.IsSecurityGroup = true;
                }
            }
            else if (this.MailEnabled) { this.IsDistributionList = true; }
        }

    }

}
