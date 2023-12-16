using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.SharePoint.Client;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Commands.SharePoint.Item;
using NovaPointLibrary.Commands.SharePoint.RecycleBin;
using NovaPointLibrary.Commands.Utilities;
using NovaPointLibrary.Solutions;
using PnP.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NovaPointLibrary
{
    public class Tester : ISolution
    {

        public static readonly string s_SolutionName = "Tester";
        public static readonly string s_SolutionDocs = "https://github.com/Barbarur/NovaPoint/wiki/";

        private TesterParameters _param = new();
        public ISolutionParameters Parameters
        {
            get { return _param; }
            set { _param = (TesterParameters)value; }
        }

        private readonly NPLogger _logger;
        private readonly AppInfo _appInfo;

        public Tester(AppInfo appInfo, Action<LogInfo> uiAddLog, TesterParameters parameters)
        {
            Parameters = parameters;
            _appInfo = appInfo;
            _logger = new(uiAddLog, this);
        }


        public async Task TestUrl(string siteUrl, string id)
        {

        }
    }

    public class TesterParameters : ISolutionParameters
    {
    }
}
