using NovaPointLibrary.Commands.Directory;
using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Core.Context;


namespace NovaPointLibrary.Solutions.Directory
{
    public class GetDirectoryGroup : ISolution
    {
        public static readonly string s_SolutionName = "Directory Groups report";
        public static readonly string s_SolutionDocs = $"https://github.com/Barbarur/NovaPoint/wiki/Solution-{typeof(GetDirectoryGroup).Name}";

        private ContextSolution _ctx;
        private readonly GetDirectoryGroupParameters _param;


        private GetDirectoryGroup(ContextSolution context, GetDirectoryGroupParameters parameters)
        {
            _ctx = context;
            _param = parameters;

            Dictionary<Type, string> solutionReports = new()
            {
                { typeof(GetDirectoryGroupRecord), "Report" },
            };
            _ctx.DbHandler.AddSolutionReports(solutionReports);
        }

        public static ISolution Create(ContextSolution context, ISolutionParameters parameters)
        {
            return new GetDirectoryGroup(context, (GetDirectoryGroupParameters)parameters);
        }

        public async Task RunAsync()
        {
            _ctx.AppClient.IsCancelled();

            string selectedProperties = "?$select=id,displayName,createdDateTime,mail,groupTypes,mailEnabled,securityEnabled,visibility,description";
            
            var collGroups = await new DirectoryGroup(_ctx.Logger, _ctx.AppClient).GetAllAsync(selectedProperties);

            ProgressTracker progress = new(_ctx.Logger, collGroups.Count());
            foreach (var group in collGroups)
            {
                
                GetDirectoryGroupRecord groupRecord = new(group);
                
                if (!_param.GroupParam.IsTargetGroup(group))
                { 
                    progress.ProgressUpdateReport();
                    continue;
                }


                try
                {
                    if (_param.GroupParam.IncludeOwners)
                    {
                        var collOwners = await new DirectoryGroupUser(_ctx.Logger, _ctx.AppClient).GetOwnersAsync(Guid.Parse(groupRecord.Id));
                        groupRecord.AddOwners(collOwners);
                    }

                    if (_param.GroupParam.IncludeMembersCount)
                    {
                        var membersTotal = await new DirectoryGroupUser(_ctx.Logger, _ctx.AppClient).GetMembersTotalCountAsync(Guid.Parse(groupRecord.Id));
                        groupRecord.AddMembersCount(membersTotal);
                    }

                }
                catch (Exception ex)
                {
                    _ctx.Logger.Error(GetType().Name, "Group", groupRecord.Id.ToString(), ex);
                    groupRecord.Remarks = ex.Message;
                }

                AddRecord(groupRecord);

                progress.ProgressUpdateReport();
            }

        }

        private void AddRecord(GetDirectoryGroupRecord record)
        {
            _ctx.DbHandler.WriteRecord(record);
        }
    }

    internal class GetDirectoryGroupRecord : ISolutionRecord
    {
        public string Id { get; set; } = String.Empty;
        public string DisplayName { get; set; } = String.Empty;
        public string Type { get; set; } = "Unknown";
        public string CreatedDate { get; set; } = String.Empty;
        public string OwnersTotal { get; set; } = "Unknown";
        public string OwnersEmail { get; set; } = "Unknown";
        public string MembershipType { get; set; } = String.Empty;

        // EXPLAIN USERS INSIDE AS MEMBERS ARE NOT INCLUDED AS IT WOULD BE A VERY LONG LIST.
        public string MembersTotal { get; set; } = "Unknown";
        public string MailEnabled { get; set; } = String.Empty;
        public string Email { get; set; } = String.Empty;
        public string SecurityEnabled { get; set; } = String.Empty;
        public string Visibility { get; set; } = String.Empty;
        public string Description { get; set; } = String.Empty;


        public string Remarks { get; set; } = string.Empty;

        public GetDirectoryGroupRecord() { }

        internal GetDirectoryGroupRecord(GraphGroup group)
        {
            Id = group.Id;
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
            MailEnabled = group.MailEnabled.ToString();
            SecurityEnabled = group.SecurityEnabled.ToString();
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
