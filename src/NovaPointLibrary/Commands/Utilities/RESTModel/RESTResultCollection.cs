using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Utilities.RESTModel
{
    internal class RESTResultCollection<T>
    {
        [JsonProperty("@odata.context")]
        public string Context { get; set; }

        [JsonProperty("nextLink")]
        public string NextLink { get; set; }

        [JsonProperty("@odata.nextLink")]
        public string ODataNextLink
        {
            get { return NextLink; }
            set { NextLink = value; }
        }

        [JsonProperty("value")]
        public IEnumerable<T> Items { get; set; }
    }
}
