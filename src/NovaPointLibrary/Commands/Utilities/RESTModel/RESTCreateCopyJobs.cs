using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.Utilities.RESTModel
{
    internal class RESTCreateCopyJobs
    {
        public string EncryptionKey { get; set; }
        public string JobId { get; set; }
        public string JobQueueUri { get; set; }
        public List<string> SourceListItemUniqueIds { get; set; }
    }
}
