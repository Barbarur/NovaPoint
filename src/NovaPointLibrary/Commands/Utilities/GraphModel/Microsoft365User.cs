using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Utilities.GraphModel
{
    internal class Microsoft365User
    {
        private string _type;
        [JsonProperty("@odata.type")]
        public string Type 
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value.Substring(value.IndexOf("graph.") + 6);
                if ( value.Contains("group") ) { UserType = "Group"; }
            }
        }

        /// <summary>
        /// Unique identifier of this user object in Azure Active Directory
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// User's user principal name
        /// </summary>
        [JsonProperty("userPrincipalName")]
        public string UserPrincipalName { get; set; }

        /// <summary>
        /// User's display name
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// User's given name
        /// </summary>
        [JsonProperty("givenName")]
        public string GivenName { get; set; }

        /// <summary>
        /// User's surname
        /// </summary>
        [JsonProperty("surname")]
        public string Surname { get; set; }

        /// <summary>
        /// User's e-mail address
        /// </summary>

        [JsonProperty("mail")]
        public string Email { get; set; }

        /// <summary>
        /// User's mobile phone number
        /// </summary>
        [JsonProperty("mobilePhone")]
        public string MobilePhone { get; set; }

        /// <summary>
        /// User's preferred language in ISO 639-1 standard notation
        /// </summary>
        [JsonProperty("preferredLanguage")]
        public string PreferredLanguage { get; set; }

        /// <summary>
        /// User's job title
        /// </summary>
        [JsonProperty("jobTitle")]
        public string JobTitle { get; set; }

        /// <summary>
        /// User's business phone numbers
        /// </summary>
        [JsonProperty("businessPhones")]
        public string[] BusinessPhones { get; set; }

        /// <summary>
        /// User's job title
        /// </summary>
        [JsonPropertyName("userType")]
        public string? UserType { get; set; } = null;

        /// <summary>
        /// Location from which Microsoft 365 will mainly be used
        /// </summary>
        [JsonProperty("usageLocation")]
        public string UsageLocation { get; set; }

        /// <summary>
        /// Aliases set on the mailbox of this user
        /// </summary>
        [JsonProperty("proxyAddresses")]
        public string[] ProxyAddresses { get; set; }
    }
}
