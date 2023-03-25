using AngleSharp.Css.Dom;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions
{
    public class LogInfo
    {
        public string ClassMethod { get; set; } = String.Empty;
        public string MainClassInfo { get; set; } = String.Empty;
        public string DetailInfo { get; set; } = String.Empty;
        public double PercentageProgress { get; set; } = 0;

        public LogInfo(string classMethod, string mainInfo = "", double percentageProgress = 0)
        {
            ClassMethod = classMethod;
            MainClassInfo = mainInfo;
            PercentageProgress = percentageProgress;
        }

        public LogInfo(string classMethod)
        {
            ClassMethod = classMethod;
        }

        public LogInfo(Action<LogInfo> logger)
        {
            logger(this);
        }



        internal void Clear()
        {
            MainClassInfo = String.Empty;
            DetailInfo = String.Empty; 
            PercentageProgress = 0;
        }
    }
}
