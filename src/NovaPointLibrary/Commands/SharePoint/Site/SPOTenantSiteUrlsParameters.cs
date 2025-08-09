using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Solutions;
using System.Linq.Expressions;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    public class SPOTenantSiteUrlsParameters : ISolutionParameters
    {
        private Expression<Func<SiteProperties, object>>[] _sitePropertiesExpressions = [];
        internal Expression<Func<SiteProperties, object>>[] SitePropertiesExpressions
        {
            get { return _sitePropertiesExpressions; }
            set
            {
                Expression<Func<SiteProperties, object>>[] defaultExpressions =
                    [
                        p => p.Title,
                        p => p.Url,
                    ];
                _sitePropertiesExpressions = [.. defaultExpressions.Union(value)];
            }
        }

        private Expression<Func<Web, object>>[] _webExpressions = [];
        internal Expression<Func<Web, object>>[] WebExpressions
        {
            get { return _webExpressions; }
            set
            {
                Expression<Func<Web, object>>[] defaultExpressions =
                    [
                        w => w.Id,
                        w => w.Title,
                        w => w.Url,
                    ];
                _webExpressions = [.. defaultExpressions.Union(value)]; 
            }
        }

        public bool ActiveSites { get; set; } = false;
        public bool IncludePersonalSite { get; set; } = false;
        public bool IncludeCommunication { get; set; } = false;
        public bool IncludeTeamSite {  get; set; } = false;
        public bool IncludeTeamSiteWithTeams { get; set; } = false;
        public bool IncludeTeamSiteWithNoGroup { get; set; } = false;
        public bool IncludeChannels { get; set; } = false;
        public bool IncludeClassic { get; set; } = false;

        
        private string _siteUrl = string.Empty;
        public string SiteUrl
        {
            get { return _siteUrl; }
            set
            { 
                _siteUrl = value.Trim();
                if (_siteUrl.EndsWith("/"))
                {
                    _siteUrl = _siteUrl.Remove(_siteUrl.LastIndexOf("/"));
                }
            }
        }


        private string _listOfSitesPath = string.Empty;
        public string ListOfSitesPath
        {
            get { return _listOfSitesPath; }
            set { _listOfSitesPath = value.Trim(); }
        }
        public bool IncludeSubsites { get; set; } = false;
    }
}
