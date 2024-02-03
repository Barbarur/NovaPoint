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

        private string _deletedByEmail = String.Empty;
        public string DeletedByEmail
        {
            get { return _deletedByEmail; }
            set { _deletedByEmail = value.Trim(); }
        }

        private string _originalLocation = String.Empty;
        public string OriginalLocation
        {
            get { return _originalLocation; }
            set { _originalLocation = value.Trim(); }
        }
        public double FileSizeMb { get; set; } = 0;
        public bool FileSizeAbove { get; set; } = true;
        public bool RenameFile { get; set; } = false;
    }
}
