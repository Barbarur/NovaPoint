using AngleSharp.Css.Dom;
using NovaPointLibrary.Commands.Directory;
using NovaPointLibrary.Commands.SharePoint.Site;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Logging;


namespace NovaPointLibrary.Solutions.Directory
{
    public class GetDirectoryGroup
    {
        public static readonly string s_SolutionName = "Directory Groups report";
        public static readonly string s_SolutionDocs = $"https://github.com/Barbarur/NovaPoint/wiki/{typeof(GetDirectoryGroup).Name}";

        private readonly GetDirectoryGroupParameters _param;
        private readonly LoggerSolution _logger;
        private readonly Commands.Authentication.AppInfo _appInfo;

        private GetDirectoryGroup(LoggerSolution logger, Commands.Authentication.AppInfo appInfo, GetDirectoryGroupParameters parameters)
        {
            _param = parameters;
            _logger = logger;
            _appInfo = appInfo;
        }

        public static async Task RunAsync(GetDirectoryGroupParameters parameters, Action<LogInfo> uiAddLog, CancellationTokenSource cancelTokenSource)
        {
            LoggerSolution logger = new(uiAddLog, typeof(GetDirectoryGroup).Name, parameters);

            try
            {
                Commands.Authentication.AppInfo appInfo = await Commands.Authentication.AppInfo.BuildAsync(logger, cancelTokenSource);

                 await new GetDirectoryGroup(logger, appInfo, parameters).RunScriptAsync();

                logger.SolutionFinish();

            }
            catch (Exception ex)
            {
                logger.SolutionFinish(ex);
            }
        }

        private async Task RunScriptAsync()
        {
            _appInfo.IsCancelled();

            string selectedProperties = "?$select=id,displayName,createdDateTime,mail,groupTypes,mailEnabled,securityEnabled,visibility,description";
            
            var collGroups = await new DirectoryGroup(_logger, _appInfo).GetAllAsync(selectedProperties);

            ProgressTracker progress = new(_logger, collGroups.Count());
            foreach (var group in collGroups)
            {
                
                SolutionGetDirectoryGroupRecord groupRecord = new(group);
                
                if (!_param.GroupParam.IsTargetGroup(group))
                { 
                    progress.ProgressUpdateReport();
                    continue;
                }


                try
                {
                    if (_param.GroupParam.IncludeOwners)
                    {
                        var collOwners = await new DirectoryGroupUser(_logger, _appInfo).GetOwnersAsync(groupRecord.Id);
                        groupRecord.AddOwners(collOwners);
                    }

                    if (_param.GroupParam.IncludeMembersCount)
                    {
                        var membersTotal = await new DirectoryGroupUser(_logger, _appInfo).GetMembersTotalCountAsync(groupRecord.Id);
                        groupRecord.AddMembersCount(membersTotal);
                    }

                }
                catch (Exception ex)
                {
                    _logger.Error(GetType().Name, "Group", groupRecord.Id.ToString(), ex);
                    groupRecord.Remarks = ex.Message;
                }

                AddRecord(groupRecord);

                progress.ProgressUpdateReport();
            }

        }

        private void AddRecord(SolutionGetDirectoryGroupRecord record)
        {
            _logger.RecordCSV(record);
        }
    }

    internal class SolutionGetDirectoryGroupRecord : ISolutionRecord
    {
        internal Guid Id { get; set; }
        internal string DisplayName { get; set; }
        internal string Type { get; set; } = "Unknown";
        internal string CreatedDate { get; set; }
        internal string OwnersTotal { get; set; } = "Unknown";
        internal string OwnersEmail { get; set; } = "Unknown";
        internal string MembershipType { get; set; }

        // EXPLAIN USERS INSIDE AS MEMBERS ARE NOT INCLUDED AS IT WOULD BE A VERY LONG LIST.
        internal string MembersTotal { get; set; } = "Unknown";
        internal string Email { get; set; }
        internal bool MailEnabled { get; set; }
        internal bool SecurityEnabled { get; set; }
        internal string Visibility { get; set; }
        internal string Description { get; set; }


        internal string Remarks { get; set; } = string.Empty;

        internal SolutionGetDirectoryGroupRecord(GraphGroup group)
        {
            Id = Guid.Parse(group.Id);
            DisplayName = group.DisplayName;

            group.DefineTypeGroup();
            if (group.IsMS365Group)
            {
                Type = "Microsoft 365 Group";
            }
            else if (group.IsEmailEnabledSecurityGroup)
            {
                Type = "Mail-enabled security group";
            }
            else if (group.IsSecurityGroup)
            {
                Type = "Security Group";
            }
            else if (group.IsDistributionList)
            {
                Type = "Distribution List";
            }
            else { Type = "Unknown"; }

            if (group.GroupTypes.Exists(s => s.Contains("Dynamic", StringComparison.OrdinalIgnoreCase)))
            {
                MembershipType = "Dynamic";
            }
            else
            {
                MembershipType = "Static";
            }

            CreatedDate = group.CreatedDateTime.ToString();

            Email = group.Email;
            MailEnabled = group.MailEnabled;
            SecurityEnabled = group.SecurityEnabled;
            Visibility = group.Visibility;
            Description = group.Description;

        }

        internal void AddOwners(IEnumerable<GraphUser> collOwners)
        {
            OwnersTotal = collOwners.Count().ToString();

            OwnersEmail = string.Join(" ", collOwners.Select(owner => owner.Email).ToList());
        }

        internal void AddMembersCount(string membersCount)
        {
            MembersTotal = membersCount;
        }

    }

    public class GetDirectoryGroupParameters(DirectoryGroupParameters groupParam) : ISolutionParameters
    {
        public DirectoryGroupParameters GroupParam { get; init; } = groupParam;

    }
}
