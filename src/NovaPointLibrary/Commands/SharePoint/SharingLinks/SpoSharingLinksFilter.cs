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

        public string FilterCreatedBy { get; set; } = string.Empty;

        internal bool MatchFilters(SpoSharingLinksRecord link)
        {
            bool typeMatch = false;
            bool permissionMatch = false;
            bool authorMatch = false;

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

            if (typeMatch && permissionMatch && authorMatch)
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
