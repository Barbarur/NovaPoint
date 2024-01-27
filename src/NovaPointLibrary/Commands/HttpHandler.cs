//using Newtonsoft.Json;
//using NovaPoint.Commands.Site;
//using NovaPointLibrary.Commands.Authentication;
//using NovaPointLibrary.Solutions;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http.Json;
//using System.Text;
//using System.Threading.Tasks;

//namespace NovaPointLibrary.Commands
//{
//    //internal class HttpHandler
//    //{
//    //    private readonly NPLogger _logger;
//    //    private readonly HttpClient HttpsClient;
//    //    private readonly string AccessToken;
//    //    internal HttpHandler(NPLogger logger, string accessToken)
//    //    {
//    //        _logger = logger;
//    //        HttpsClient = new();
//    //        AccessToken = accessToken;
//    //    }

//    //    public async Task<string> SPO_Get(string message)
//    //    {

//    //        var httpRequest = new HttpRequestMessage(HttpMethod.Get,
//    //            message);
//    //        httpRequest.Headers.Add("Accept", "application/json;odata=nometadata");
//    //        httpRequest.Headers.Add("Authorization", "Bearer " + AccessToken);

//    //        _logger.AddLogToTxt($"SendingAsync");
//    //        HttpResponseMessage response = await HttpsClient.SendAsync(httpRequest);
//    //        string jsonResponse = await response.Content.ReadAsStringAsync();

//    //        if (response.IsSuccessStatusCode)
//    //        {
//    //            _logger.AddLogToTxt($"Successful response");
//    //            return jsonResponse;
//    //        }
//    //        else
//    //        {
//    //            _logger.AddLogToTxt($"Error response");
//    //            SharePointException? responseContent = JsonConvert.DeserializeObject<SharePointException>(jsonResponse);
//    //            string exception = responseContent.ErrorData.Message.Value;
//    //            throw new Exception(exception);
//    //        }
//    //    }

//    //}
//    //internal class SharePointException
//    //{
//    //    [JsonProperty("odata.error")]
//    //    public ErrorData? ErrorData { get; set; }
//    //}
//    //internal class ErrorData
//    //{
//    //    [JsonProperty("code")]
//    //    public string? Code { get; set; }
//    //    [JsonProperty("message")]
//    //    public Message? Message { get; set; }
//    //}
//    //internal class Message
//    //{
//    //    [JsonProperty("lang")]
//    //    public string? Lang { get; set; }
//    //    [JsonProperty("value")]
//    //    public string? Value { get; set; }
//    //}
//    //internal class ContentResponseGraphAdminList
//    //{
//    //    [JsonProperty("@odata.context")]
//    //    public string odatacontext { get; set; }

//    //    [JsonProperty("@odata.nextLink")]
//    //    public string odatanextLink { get; set; }
//    //    public List<GraphAdminListSite> value { get; set; }
//    //}
//}
