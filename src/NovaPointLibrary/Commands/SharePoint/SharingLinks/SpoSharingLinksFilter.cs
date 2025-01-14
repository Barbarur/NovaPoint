using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;


namespace NovaPointLibrary.Commands.SharePoint.SharingLinks
{
    public class SpoSharingLinksFilter : ISolutionParameters
    {
        public bool IncludeAnyone { get; set; } = false;
        public bool IncludeOrganization { get; set; } = false;
        public bool IncludeSpecific { get; set; } = false;

        public bool IncludeCanEdit { get; set; } = false;
        public bool IncludeCanReview { get; set; } = false;
        public bool IncludeCanNotDownload { get; set; } = false;
        public bool IncludeCanView { get; set; } = false;

        private string _filterCreatedBy = string.Empty;
        public string FilterCreatedBy
        {
            get { return _filterCreatedBy; }
            set { _filterCreatedBy = value.Trim(); }
        }

        public int DaysOld { get; set; } = 0;

        public void ParametersCheck()
        {
            if (DaysOld < 0)
            {
                throw new("Parameter 'Older than' should be 0 or above");
            }
        }

        internal bool MatchFilters(SpoSharingLinksRecord link)
        {
            bool typeMatch = false;
            bool permissionMatch = false;
            bool authorMatch = false;
            bool age = false;

            if (link.LinkDetailsAnonymous)
            {
                if (IncludeAnyone) { typeMatch = true; }
            }
            else if (link.LinkDetailsOrganization)
            {
                if (IncludeOrganization) { typeMatch = true; }
            }
            else
            {
                if (IncludeSpecific) { typeMatch = true; }
            }

            if (link.LinkDetailsCanEdit)
            {
                if (IncludeCanEdit) { permissionMatch = true; }
            }
            else if (link.LinkDetailsCaReview)
            {
                if (IncludeCanReview) { permissionMatch = true; }
            }
            else if (link.LinkDetailsCanNotDownload)
            {
                if (IncludeCanNotDownload) { permissionMatch = true; }
            }
            else
            {
                if (IncludeCanView) { permissionMatch = true; }
            }

            if (string.IsNullOrWhiteSpace(FilterCreatedBy))
            {
                authorMatch = true;
            }
            else if (link.SharingLinkCreatedBy.Equals(FilterCreatedBy, StringComparison.OrdinalIgnoreCase))
            {
                authorMatch = true;
            }

            DateTime createdBeforeThan = DateTime.Today.AddDays(DaysOld * -1);
            if (link.SharingLinkCreated <= createdBeforeThan)
            {
                age = true;
            }


            if (typeMatch && permissionMatch && authorMatch && age)
            {
                return true;
            }
            else
            {
                return false;
            }

        }
    }
}
