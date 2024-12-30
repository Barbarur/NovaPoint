using Microsoft.Graph;
using NovaPointLibrary.Solutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    public class SPOTenantSiteUrlsParameters : ISolutionParameters
    {
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
