using NovaPointLibrary.Commands.Utilities.GraphModel;
using NovaPointLibrary.Commands.Utilities.RESTModel;
using NovaPointLibrary.Solutions;

namespace NovaPointLibrary.Commands.Directory
{
    public class DirectoryGroupParameters : ISolutionParameters
    {
        public DateTime CreatedAfter { get; set; } = DateTime.MinValue;
        public DateTime CreatedBefore { get; set; } = DateTime.MaxValue;
        public bool IncludeMS365 { get; set; } = true;
        public bool IncludeSecurity { get; set; } = true;
        public bool IncludeEmailSecurity { get; set; } = true;
        public bool IncludeDistributionList { get; set; } = true;


        public bool IncludeOwners { get; set; } = false;
        public bool IncludeMembersCount { get; set; } = false;


        //private string _listOfUserUpn = string.Empty;
        //public string ListOfUserUpn
        //{
        //    get { return _listOfUserUpn; }
        //    set { _listOfUserUpn = value.Trim(); }
        //}


        internal bool IsTargetGroup(GraphGroup group)
        {
            if (group.CreatedDateTime < this.CreatedAfter) {  return false; }

            if (this.CreatedBefore < group.CreatedDateTime) { return false; }

            if (group.IsMS365Group)
            { 
                if (this.IncludeMS365) { return true; } 
            }
            else if (group.IsEmailEnabledSecurityGroup)
            {
                if (this.IncludeEmailSecurity) { return true; }
            }
            else if (group.IsSecurityGroup)
            {
                if (this.IncludeSecurity) { return true; }
            }
            else if (group.IsDistributionList)
            {
                if (this.IncludeDistributionList) { return true; }
            }
            else { return true; }
            return false;
        }

    }
}
