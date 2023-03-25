using Newtonsoft.Json;
using NovaPoint.Commands.Site;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands
{
    internal class HttpHandler
    {
        private LogHelper _logHelper;
        private readonly HttpClient HttpsClient;
        private readonly string AccessToken;
        internal HttpHandler(LogHelper logHelper, string accessToken)
        {
            _logHelper = logHelper;
            HttpsClient = new();
            AccessToken = accessToken;
        }
        public static async Task<string> Graph_Get(string message, string accessToken)
        {
            HttpResponseMessage? response;

            var httpClient = new HttpClient();
            var httpRequest = new HttpRequestMessage(HttpMethod.Get,
                message);

            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", accessToken);

            response = await httpClient.SendAsync(httpRequest);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("HTTPHandler Successful response");
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                Console.WriteLine("Error at HTTPHandler");
                string exception = await response.Content.ReadAsStringAsync();
                throw new Exception(exception);
            }
        }

        public async Task<string> SPO_Get(string message)
        {
            _logHelper = new(_logHelper, $"{GetType().Name}.SPO_Get");

            var httpRequest = new HttpRequestMessage(HttpMethod.Get,
                message);
            httpRequest.Headers.Add("Accept", "application/json;odata=nometadata");
            httpRequest.Headers.Add("Authorization", "Bearer " + AccessToken);

            _logHelper.AddLogToTxt($"SendingAsync");
            HttpResponseMessage response = await HttpsClient.SendAsync(httpRequest);
            string jsonResponse = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logHelper.AddLogToTxt($"Successful response");
                return jsonResponse;
            }
            else
            {
                _logHelper.AddLogToTxt($"Error response");
                SharePointException? responseContent = JsonConvert.DeserializeObject<SharePointException>(jsonResponse);
                string exception = responseContent.ErrorData.Message.Value;
                throw new Exception(exception);
            }
        }

        public static async Task SPO_Merge(string accessToken)
        {
            //url: "<app web url>/_api/SP.AppContextSite(@target)/web/siteusers(@v)? @v = 'i%3A0%23.f%7Cmembership%7Cuser%40domain.onmicrosoft.com'&@target='<host web url>'";
            //i: 0#.f|membership|user@domain.com
            //…/ users(@v) ? @v = 'i%3A0%23.f%7Cmembership%7Cuser%40domain.onmicrosoft.com'
            //alexw@M365x29094319.onmicrosoft.com
            //i%3A0%23.f%7Cmembership%7Calexw%40M365x29094319.onmicrosoft.com'


            //string apiUrl = "https://m365x29094319-admin.sharepoint.com/_api/SP.AppContextSite(@target)/web/siteusers(@v)?@v='i%3A0%23.f%7Cmembership%7Calexw%40M365x29094319.onmicrosoft.com'&@target='https://m365x29094319.sharepoint.com/sites/SiteN1'";
            string apiUrl = "https://m365x29094319-admin.sharepoint.com/_api/SP.AppContextSite(@target)/web/siteusers(@v)?@v='i%3A0%23.f%7Cmembership%7Calexw%40M365x29094319.onmicrosoft.com'&@target='https://m365x29094319.sharepoint.com/sites/AllanSite'";

            string message = "{ '__metadata': { 'type': 'SP.User' }, 'Email':'alexw@M365x29094319.onmicrosoft.com', 'IsSiteAdmin':true }";

            Console.WriteLine("HTTPHandler SPO_Merge Start");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, message);
            //httpRequest.Headers.Add("Authorization", "Bearer " + accessToken);
            //httpRequest.Headers.Add("content-type", "application/json; odata=verbose");
            //httpRequest.Headers.Add("X-HTTP-Method", "MERGE");

            HttpClientHandler handler = new();

            //HttpContent httpContent = new("{ '__metadata': { 'type': 'SP.User' }, 'Email':'user2@domain.com', 'IsSiteAdmin':false, 'Title':'User 2' }");
            StringContent stringContent = new(message);
            //stringContent.Headers.Add("Authorization", "Bearer " + accessToken);
            stringContent.Headers.Clear();
            stringContent.Headers.Add("content-type", "application/json; odata=verbose");
            stringContent.Headers.Add("X-HTTP-Method", "MERGE");

            //JsonContent jsonContent = new("{ '__metadata': { 'type': 'SP.User' }, 'Email':'user2@domain.com', 'IsSiteAdmin':false, 'Title':'User 2' }");
            JsonContent jsonContent = JsonContent.Create("{ '__metadata': { 'type': 'SP.User' }, 'Email':'alexw@M365x29094319.onmicrosoft.com', 'IsSiteAdmin':false}");
            //jsonContent.headers  .Add("Authorization", "Bearer " + accessToken);
            jsonContent.Headers.Clear();
            jsonContent.Headers.Add("content-type", "application/json; odata=verbose");
            jsonContent.Headers.Add("X-HTTP-Method", "MERGE");

            HttpClient httpClient = new();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
            //httpClient.DefaultRequestHeaders.Add("content-type", "application/json; odata=verbose");
            //httpClient.DefaultRequestHeaders.Add("X-HTTP-Method", "MERGE");

            HttpResponseMessage response = await httpClient.PostAsync(apiUrl, stringContent);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("HTTPHandler SPO_Merge Successful response");
                return;
            }
            else
            {
                Console.WriteLine("HTTPHandler SPO_Merge Error response");
                string exception = await response.Content.ReadAsStringAsync();
                throw new Exception(exception);
            }
        }

    }
    internal class SharePointException
    {
        [JsonProperty("odata.error")]
        public ErrorData? ErrorData { get; set; }
    }
    internal class ErrorData
    {
        [JsonProperty("code")]
        public string? Code { get; set; }
        [JsonProperty("message")]
        public Message? Message { get; set; }
    }
    internal class Message
    {
        [JsonProperty("lang")]
        public string? Lang { get; set; }
        [JsonProperty("value")]
        public string? Value { get; set; }
    }
    internal class ContentResponseGraphAdminList
    {
        [JsonProperty("@odata.context")]
        public string odatacontext { get; set; }

        [JsonProperty("@odata.nextLink")]
        public string odatanextLink { get; set; }
        public List<GraphAdminListSite> value { get; set; }
    }
}
