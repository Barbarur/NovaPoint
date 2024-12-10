using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Commands.SharePoint.Site
{
    internal class SPOWeb
    {

        internal static string GetSiteTemplateName(string template, bool isTeamsConnected)
        {
            string templateName = template;
            if (template.Contains("SPSPERS", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "OneDrive";
            }
            else if (template.Contains("SITEPAGEPUBLISHING#0", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Communication site";
            }
            else if (template.Contains("GROUP#0", StringComparison.OrdinalIgnoreCase))
            {
                if (isTeamsConnected) { templateName = "Team site connected to MS Teams"; }
                else { templateName = "Team site"; }
            }
            else if (template.Contains("STS#3", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Team site (no Microsoft 365 group)";
            }
            else if (template.Contains("STS#0", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Team site (classic experience)";
            }
            else if (template.Contains("TEAMCHANNEL", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Channel site";
            }
            else if (template.Contains("APPCATALOG", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "App Catalog Site";
            }
            else if (template.Contains("STS", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Team site (Subsite)";
            }
            else if (template.Contains("PROJECTSITE", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Project site (Subsite)";
            }
            else if (template.Contains("SRCHCENTERLITE", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Basic Search Center (Subsite)";
            }
            else if (template.Contains("BDR", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Document Center (Subsite)";
            }
            else if (template.Contains("SAPWORKFLOWSITE", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "SAP Workflow site (Subsite)";
            }
            else if (template.Contains("VISPRUS", StringComparison.OrdinalIgnoreCase))
            {
                templateName = "Visio Process Repository (Subsite)";
            }

            return templateName;
        }
    }
}
