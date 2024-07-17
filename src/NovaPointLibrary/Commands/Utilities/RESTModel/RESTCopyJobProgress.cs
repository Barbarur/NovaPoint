using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Utilities.RESTModel
{
    internal class RESTCopyJobProgress
    {
        [JsonProperty("odata.metadata")]
        public string Metadata { get; set; } = string.Empty;
        public int JobState { get; set; }
        public List<string> Logs { get; set; } = new List<string>();
    }
}
