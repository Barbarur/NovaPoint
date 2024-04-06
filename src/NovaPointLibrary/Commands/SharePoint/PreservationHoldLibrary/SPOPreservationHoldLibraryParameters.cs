using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.PreservationHoldLibrary
{
    public class SPOPreservationHoldLibraryParameters : ISolutionParameters
    {
        public bool AllItems { get; set; } = true;

        public bool RetainedByDate { get; set; } = false;
        public DateTime RetainedAfterDate { get; set; } = DateTime.MinValue;
        public DateTime RetainedBeforeDate { get; set; } = DateTime.MaxValue;

        private string _itemName = String.Empty;
        public string ItemName
        {
            get { return _itemName; }
            set { _itemName = value.Trim(); }
        }

        private string _originalLocation = String.Empty;
        public string OriginalLocation
        {
            get { return _originalLocation; }
            set { _originalLocation = value.Trim(); }
        }

        private string _modifiedByEmail = String.Empty;
        public string ModifiedByEmail
        {
            get { return _modifiedByEmail; }
            set { _modifiedByEmail = value.Trim(); }
        }
        public double AboveFileSizeMb { get; set; } = 0;


        internal bool MatchParameters(ListItem item)
        {
            if (AllItems) { return true; }
            else
            {
                bool match;

                bool date;
                if (RetainedByDate)
                {
                    DateTime itemDateTime = (DateTime)item["PreservationDatePreserved"];
                    if (RetainedAfterDate.CompareTo(itemDateTime) <= 0 && 0 >= RetainedBeforeDate.CompareTo(itemDateTime))
                    {
                        date = true;
                    }
                    else { date = false; }
                }
                else { date = true; }

                bool name;
                if (!string.IsNullOrWhiteSpace(ItemName))
                {
                    string itemName = (string)item["FileLeafRef"];
                    if (itemName.Contains(ItemName)) { name = true; }
                    else { name = false; }
                }
                else { name = true; }

                bool location;
                if (!string.IsNullOrWhiteSpace(OriginalLocation))
                {
                    string originalPath = (string)item["PreservationOriginalURL"];
                    if (originalPath.Contains(OriginalLocation)) { location = true; }
                    else { location = false; }
                }
                else { location = true; }

                bool email;
                if (!string.IsNullOrWhiteSpace(ModifiedByEmail))
                {
                    FieldUserValue? author = (FieldUserValue)item["Author"];
                    if ( author != null && ModifiedByEmail.Equals(author.Email, StringComparison.OrdinalIgnoreCase)) { email = true; }
                    else { email = false; }
                }
                else { email = true; }

                bool size;
                if (AboveFileSizeMb > 0)
                {
                    FieldLookupValue? FileSizeTotalBytes = (FieldLookupValue)item["SMTotalSize"];
                    if (FileSizeTotalBytes != null && (float)Math.Round(FileSizeTotalBytes.LookupId / Math.Pow(1024, 2), 2) > AboveFileSizeMb) { size = true; }
                    else { size = false; }
                }
                else { size = true; }

                if (date && name && location && email && size) { match = true; }
                else { match = false; }

                return match;
            }
        }
    }
}
