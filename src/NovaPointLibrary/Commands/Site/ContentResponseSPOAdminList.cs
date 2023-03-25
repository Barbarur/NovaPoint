using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPoint.Commands.Site
{
    internal class ContentResponseSPOAdminList
    {
        [JsonProperty("odata.nextLink")]
        public string odatanextLink { get; set; }
        public List<SPOAdminListSite> value { get; set; }
    }
    internal class SPOAdminListSite
    {
        private string _title;
        public string Title
        {
            get { return _title; }
            set { if (value == null) { _title = ""; } else { _title = value; } }
        }

        public int FileSystemObjectType { get; set; }
        public int Id { get; set; }
        public object ServerRedirectedEmbedUri { get; set; }
        public string ServerRedirectedEmbedUrl { get; set; }
        public string ContentTypeId { get; set; }

        public object ComplianceAssetId { get; set; }
        public int ConditionalAccessPolicy { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedByEmail { get; set; }
        public object DeletedBy { get; set; }
        public object IBSegmentsGuids { get; set; }
        public object Initiator { get; set; }
        public object IsRestorable { get; set; }
        public object LastItemModifiedDate { get; set; }
        public DateTime? LastListActivityOn { get; set; }
        public object LastWebActivityOn { get; set; }
        public object NumOfFiles { get; set; }
        public object OperationStartTime { get; set; }
        public string RootWebId { get; set; }
        public object SensitivityLabel { get; set; }
        public bool ShareByEmailEnabled { get; set; }
        public bool ShareByLinkEnabled { get; set; }
        public int SiteFlags { get; set; }
        public string SiteId { get; set; }
        public string SiteOwnerEmail { get; set; }
        public string SiteOwnerName { get; set; }
        public string SiteUrl { get; set; }
        public object State { get; set; }
        public double StorageQuota { get; set; }
        public double StorageUsed { get; set; }
        public double StorageUsedPercentage { get; set; }
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public DateTime TimeCreated { get; set; }
        public object TimeDeleted { get; set; }
        public bool WasSegmentApplied { get; set; }
        public int ID { get; set; }
        public DateTime Modified { get; set; }
        public DateTime Created { get; set; }
        public int AuthorId { get; set; }
        public int EditorId { get; set; }
        public string OData__UIVersionString { get; set; }
        public bool Attachments { get; set; }
        public string GUID { get; set; }
    }
}
