using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Utilities
{
    public class VersionControl
    {

        public static async Task<bool> IsUpdated()
        {
            string? versionAssembly = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string? versionGitHub = await GetGithubLatestRelease();

            if (versionAssembly != null && versionGitHub != null)
            {
                String[] result = versionAssembly.Split('.').ToArray();
                versionAssembly = string.Join(".", result, 0, 3);

                if (String.Equals(versionAssembly, versionGitHub)) { return true; }
                else { return false; }
            }
            else
            {
                return false;
            }
        }

        private static async Task<string?> GetGithubLatestRelease()
        {
            HttpClient httpsClient = new();

            HttpRequestMessage requestMessage = new(HttpMethod.Get, "https://api.github.com/repos/Barbarur/NovaPoint/releases/latest");
            requestMessage.Headers.Add("User-Agent", "NovaPoint");

            HttpResponseMessage responseMessage = await httpsClient.SendAsync(requestMessage);

            if (responseMessage.IsSuccessStatusCode)
            {
                var responseContent = await responseMessage.Content.ReadAsStringAsync();

                GithubLatestRelease? response = JsonConvert.DeserializeObject<GithubLatestRelease>(responseContent);
                if (response != null) { return response.TagName; }
                else { return null; }
            }
            else { return null; }
        }
    }


    internal class GithubLatestRelease
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; } = string.Empty;
    }
}
