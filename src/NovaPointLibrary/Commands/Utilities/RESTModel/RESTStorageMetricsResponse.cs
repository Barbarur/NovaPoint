using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Utilities.RESTModel
{
    internal class RESTStorageMetricsResponse
    {
        [JsonProperty("odata.metadata")]
        public string odatametadata { get; set; }

        [JsonProperty("odata.type")]
        public string odatatype { get; set; }

        [JsonProperty("odata.id")]
        public string odataid { get; set; }

        [JsonProperty("odata.editLink")]
        public string odataeditLink { get; set; }

        [JsonProperty("StorageMetrics@odata.navigationLinkUrl")]
        public string StorageMetricsodatanavigationLinkUrl { get; set; }
        public RESTStorageMetrics StorageMetrics { get; set; }
    }

    public class RESTStorageMetrics
    {
        [JsonProperty("odata.type")]
        public string odatatype { get; set; }

        [JsonProperty("odata.id")]
        public string odataid { get; set; }

        [JsonProperty("odata.editLink")]
        public string odataeditLink { get; set; }
        public string AdditionalFileStreamSize { get; set; }
        public DateTime LastModified { get; set; }
        public long TotalFileCount { get; set; }
        public long TotalFileStreamSize { get; set; }
        public long TotalSize { get; set; }
    }
}
