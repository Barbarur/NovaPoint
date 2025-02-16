using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.List;
using NovaPointLibrary.Commands.SharePoint.Permision.Utilities;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Core.Logging;
using NovaPointLibrary.Solutions;
using System.Data;
using System.Linq.Expressions;

namespace NovaPointLibrary.Commands.SharePoint.Permision
{
    internal class SPOSitePermissionsCSOM
    {
        private readonly LoggerSolution _logger;
        private readonly AppInfo _appInfo;
        private readonly SPOSitePermissionsCSOMParameters _param;

        private readonly SPOKnownRoleAssignmentGroups _knownGroups = new();

        private readonly Expression<Func<Web, object>>[] siteExpressions = new Expression<Func<Web, object>>[]
        {
            w => w.HasUniqueRoleAssignments,
            w => w.Id,
            w => w.RoleAssignments.Include(
                ra => ra.RoleDefinitionBindings,
                ra => ra.Member),
            w => w.Title,
            w => w.Url,
        };

        private readonly Expression<Func<Microsoft.SharePoint.Client.List, object>>[] _listExpresions = new Expression<Func<Microsoft.SharePoint.Client.List, object>>[]
        {
            l => l.BaseType,
            l => l.DefaultViewUrl,
            l => l.HasUniqueRoleAssignments,
            l => l.Hidden,
            l => l.Id,
            l => l.ItemCount,
            l => l.RoleAssignments.Include(
                ra => ra.RoleDefinitionBindings,
                ra => ra.Member),
            l => l.Title,
        };

        private readonly Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] _fileExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
        {
            f => f.HasUniqueRoleAssignments,
            f => f["ID"],
            f => f.FileSystemObjectType,
            f => f["FileLeafRef"],
            f => f["FileRef"],
            f => f.RoleAssignments.Include(
                ra => ra.RoleDefinitionBindings,
                ra => ra.Member),
            f => f.Versions,
        };

        private readonly Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[] _itemExpressions = new Expression<Func<Microsoft.SharePoint.Client.ListItem, object>>[]
        {
            i => i.HasUniqueRoleAssignments,
            i => i["ID"],
            i => i.FileSystemObjectType,
            i => i["FileRef"],
            i => i.RoleAssignments.Include(
                ra => ra.RoleDefinitionBindings,
                ra => ra.Member),
            i => i["Title"],
            i => i.Versions,
        };

        public SPOSitePermissionsCSOM(LoggerSolution logger, AppInfo appInfo, SPOSitePermissionsCSOMParameters parameters)
        {
            _param = parameters;
            _param.ListsParam.ListExpressions = _listExpresions;
            _param.ItemsParam.FileExpresions = _fileExpressions;
            _param.ItemsParam.ItemExpresions = _itemExpressions;
            _logger = logger;
            _appInfo = appInfo;
        }




        internal async IAsyncEnumerable<SPOLocationPermissionsRecord> GetAsync(string siteUrl, ProgressTracker parentProgress)
        {
            _appInfo.IsCancelled();

            Web oSite = await new SPOWebCSOM(_logger, _appInfo).GetAsync(siteUrl, siteExpressions);

            if(!oSite.IsSubSite() && _param.IncludeAdmins)
            {
                await foreach (var record in GetSiteAdminAsync(oSite))
                {
                    yield return record;
                }
            }

            if (_param.IncludeSiteAccess)
            {
                if(oSite.IsSubSite() && !oSite.HasUniqueRoleAssignments)
                {
                    yield return new("Site", oSite.Title, oSite.Url, SPORoleAssignmentUserRecord.GetRecordInherits());
                }
                else
                {
                    await foreach (var record in GetSiteAccessAsync(oSite))
                    {
                        yield return record;
                    }
                }
            }

            if(_param.IncludeUniquePermissions)
            {
                await foreach(var record in GetUniquePermissionsAsync(oSite, parentProgress))
                {
                    yield return record;
                }
            }
        }

        internal async IAsyncEnumerable<SPOLocationPermissionsRecord> GetSiteAdminAsync(Web oSite)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting Site collection admins for Site '{oSite.Url}'");

            string accessType = "Direct Permissions";
            string permissionLevels = "Site Collection Administrator";

            IEnumerable<Microsoft.SharePoint.Client.User>? collSiteCollAdmins = null;
            string exceptionMessage = string.Empty;
            try
            {
                collSiteCollAdmins = await new SPOSiteCollectionAdminCSOM(_logger, _appInfo).GetAsync(oSite.Url);

                if(!collSiteCollAdmins.Any())
                {
                    exceptionMessage = "No Site Collection Admins found";
                }
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "Site", oSite.Url, ex);
                exceptionMessage = ex.Message;
            }

            if(String.IsNullOrWhiteSpace(exceptionMessage) && collSiteCollAdmins != null)
            {
                string users = String.Join(" ", collSiteCollAdmins.Where(sca => sca.PrincipalType.ToString() == "User").Select(sca => sca.UserPrincipalName).ToList());
                if (!string.IsNullOrWhiteSpace(users))
                {
                    yield return new("Site", oSite.Title, oSite.Url, SPORoleAssignmentUserRecord.GetRecordUserDirectPermissions(users, permissionLevels));
                }


                var collSecurityGroups = collSiteCollAdmins.Where(gm => gm.PrincipalType.ToString() == "SecurityGroup").ToList();
                await foreach (var role in new SPORoleAssignmentUsersCSOM(_logger, _appInfo, _knownGroups).GetSecurityGroupUsersAsync(collSecurityGroups, accessType, permissionLevels))
                {
                    yield return new("Site", oSite.Title, oSite.Url, role);
                }
            }
            else
            {
                yield return new("Site", oSite.Title, oSite.Url, SPORoleAssignmentUserRecord.GetRecordBlankException(exceptionMessage));
                yield break;
            }
            
        }

        internal async IAsyncEnumerable<SPOLocationPermissionsRecord> GetSiteAccessAsync(Web oSite)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting Site permissions for Site '{oSite.Url}'");

            await foreach(var role in new SPORoleAssignmentUsersCSOM(_logger, _appInfo, _knownGroups).GetAsync(oSite.Url, oSite.RoleAssignments))
            {
                yield return new("Site", oSite.Title, oSite.Url, role);

            }
        }

        internal async IAsyncEnumerable<SPOLocationPermissionsRecord> GetUniquePermissionsAsync(Web oSite, ProgressTracker parentProgress)
        {
            _appInfo.IsCancelled();
            _logger.Info(GetType().Name, $"Getting Unique permissions for Site '{oSite.Url}'");

            List<Microsoft.SharePoint.Client.List>? collLists = null;
            string exceptionMessage = string.Empty;
            try
            {
                collLists = await new SPOListCSOM(_logger, _appInfo).GetAsync(oSite.Url, _param.ListsParam);
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, "Site", oSite.Url, ex);
                exceptionMessage = ex.Message;
            }

            if (String.IsNullOrEmpty(exceptionMessage) && collLists != null)
            {
                await foreach(var record in GetListsPermissionsAsync(oSite, collLists, parentProgress))
                {
                    yield return record;
                }
            }
            else
            {
                yield return new("Site", oSite.Title, oSite.Url, SPORoleAssignmentUserRecord.GetRecordBlankException(exceptionMessage));
                yield break;
            }
        }

        private async IAsyncEnumerable<SPOLocationPermissionsRecord> GetListsPermissionsAsync(Web oSite, List<Microsoft.SharePoint.Client.List> collLists, ProgressTracker parentProgress)
        {
            _appInfo.IsCancelled();

            ProgressTracker progress = new(parentProgress, collLists.Count);
            foreach (var oList in collLists)
            {
                _logger.Info(GetType().Name, $"Getting permissions for List '{oList.Title}'");

                if (oList.HasUniqueRoleAssignments)
                {
                    await foreach (var role in new SPORoleAssignmentUsersCSOM(_logger, _appInfo, _knownGroups).GetAsync(oSite.Url, oList.RoleAssignments))
                    {
                        yield return new($"{oList.BaseType}", oList.Title, $"{oList.DefaultViewUrl}", role);
                    }
                }
                else
                {
                    yield return new($"{oList.BaseType}", oList.Title, $"{oList.DefaultViewUrl}", SPORoleAssignmentUserRecord.GetRecordInherits());
                }

                foreach(var record in await GetItemsPermissionsAsync(oSite, oList, progress))
                {
                    yield return record;
                }

                progress.ProgressUpdateReport();
            }
        }

        private async Task<List<SPOLocationPermissionsRecord>> GetItemsPermissionsAsync(Web oSite, Microsoft.SharePoint.Client.List oList, ProgressTracker parentProgress)
        {
            _appInfo.IsCancelled();

            List<SPOLocationPermissionsRecord> recordsList = new() { };

            try
            {
                ProgressTracker progress = new(parentProgress, oList.ItemCount);
                await foreach (ListItem oItem in new SPOListItemCSOM(_logger, _appInfo).GetAsync(oSite.Url, oList, _param.ItemsParam))
                {
                    if (oItem.HasUniqueRoleAssignments)
                    {
                        _logger.Info(GetType().Name, $"Getting permissions for {oItem.FileSystemObjectType} '{oItem["FileRef"]}'");

                        await foreach (var role in new SPORoleAssignmentUsersCSOM(_logger, _appInfo, _knownGroups).GetAsync(oSite.Url, oItem.RoleAssignments))
                        {
                            // TO EDIT TO AVOID ISSUES WITH ITEMS TITLE
                            recordsList.Add(new($"{oItem.FileSystemObjectType}", $"{oItem["FileLeafRef"]}", $"{oItem["FileRef"]}", role));
                        }
                    }

                    progress.ProgressUpdateReport();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(GetType().Name, $"{oList.BaseType}", oList.Title, ex);

                recordsList.Add(new($"{oList.BaseType}", oList.Title, $"{oList.DefaultViewUrl}", SPORoleAssignmentUserRecord.GetRecordBlankException(ex.Message)));
            }

            return recordsList;

        }
    }

    public class SPOSitePermissionsCSOMParameters : ISolutionParameters
    {
        public bool IncludeAdmins { get; set; } = false;
        public bool IncludeSiteAccess { get; set; } = false;
        public bool IncludeUniquePermissions { get; set; } = false;
        public SPOListsParameters ListsParam { get; set; }
        public SPOItemsParameters ItemsParam { get; set; }
        public SPOSitePermissionsCSOMParameters(SPOListsParameters listParam, SPOItemsParameters itemParam)
        {
            ListsParam = listParam;
            ItemsParam = itemParam;
        }
    }
}
