using NovaPointLibrary;
using NovaPointLibrary.Commands.Authentication;
using NovaPointLibrary.Solutions;
using System.Xml.Linq;
using System;
using NovaPointLibrary.Solutions.Reports;
using NovaPointLibrary.Solutions.Report;

namespace Tester
{
    internal class Program
    {

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            AppInfo appInfo = new("M365x88421522",
                "6211e9b2-aab7-472b-a320-8d5bb52ec068",
                "dd24a4df-d34f-4549-aeea-3bce9fddfca3",
                true);

            SiteAllReportParameters parameters = new()
            {
                IncludeAdmins = true,
                AdminUPN = "admin@M365x88421522.onmicrosoft.com",
                RemoveAdmin = true,

                IncludePersonalSite = true,
                IncludeShareSite = true,
                GroupIdDefined = false,

                IncludeSubsites = true
            };

            await new SiteAllReport(UILog, appInfo, parameters).RunAsync();
            
        }
        public static void UILog(LogInfo logInfo)
        {
            if (!string.IsNullOrEmpty(logInfo.MainClassInfo)) { Console.WriteLine( logInfo.MainClassInfo + "\n" ); }
            
        }
    }
}