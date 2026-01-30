using Newtonsoft.Json;

namespace NovaPointLibrary.Commands.Utilities.GraphModel
{
    internal class GraphGroup
    {
        [JsonProperty("@odata.context")]
        public string? Context { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("createdDateTime")]
        public DateTime CreatedDateTime { get; set; }

        [JsonProperty("mail")]
        public string Email { get; set; }

        [JsonProperty("groupTypes")]
        public List<string> GroupTypes { get; set; }

        [JsonProperty("mailEnabled")]
        public bool MailEnabled { get; set; }

        [JsonProperty("securityEnabled")]
        public bool SecurityEnabled { get; set; }

        [JsonProperty("visibility")]
        public string Visibility { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        public bool IsMS365Group { get; set; } = false;
        public bool IsEmailEnabledSecurityGroup { get; set; } = false;
        public bool IsSecurityGroup { get; set; } = false;
        public bool IsDistributionList { get; set; } = false;

        internal void DefineTypeGroup()
        {
            if (this.GroupTypes != null && this.GroupTypes.Contains("Unified")) { this.IsMS365Group = true; }
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
