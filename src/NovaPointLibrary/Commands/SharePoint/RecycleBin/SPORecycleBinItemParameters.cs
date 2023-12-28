using NovaPointLibrary.Commands.SharePoint.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.RecycleBin
{
    public class SPORecycleBinItemParameters : SPOTenantSiteUrlsParameters
    {
        public bool AllItems { get; set; } = false;
        public bool FirstStage { get; set; } = true;
        public bool SecondStage { get; set; } = true;
        public DateTime DeletedAfter { get; set; } = DateTime.MinValue;
        public DateTime DeletedBefore { get; set; } = DateTime.MaxValue;
        public string DeletedByEmail { get; set; } = String.Empty;
        public string OriginalLocation { get; set; } = String.Empty;
        public double FileSizeMb { get; set; } = 0;
        public bool FileSizeAbove { get; set; } = true;
        public bool RenameFile { get; set; } = false;

        internal SPOTenantSiteUrlsParameters GetSiteParameters()
        {
            return this;
        }
    }
}
