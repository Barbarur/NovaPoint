using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions
{
    internal class ProgressTracker
    {
        private LogHelper LogHelper;
        private float MainCounter;
        private float MainTotalCount;
        private float CounterStep;
        private float SubTaskCounter = 0;
        private float SubTotalCount = 1;
        
        internal ProgressTracker(LogHelper logHelper, float totalCount)
        {
            LogHelper = logHelper;
            MainCounter = 0;
            MainTotalCount = totalCount;
            CounterStep = 1 / totalCount;

        }

        internal void MainReportProgress(string msg)
        {
            double progress = Math.Round(MainCounter * 100 / MainTotalCount, 2);
            LogHelper.AddProgressToUI(progress);
            LogHelper.AddLogToUI(msg);
        }

        internal void MainCounterIncrement()
        {
            MainCounter++;
        }

        internal void SubTaskProgressReset(float subTaskCount)
        {
            SubTotalCount = 1 + subTaskCount;
            SubTaskCounter = 0;
        }

        internal void SubTaskReportProgress(string msg)
        {
            var progress = Math.Round( ((MainCounter / MainTotalCount) + ( SubTaskCounter * CounterStep / SubTotalCount)) * 100, 2);
            LogHelper.AddProgressToUI(progress);
            LogHelper.AddLogToUI(msg);
        }

        internal void SubTaskCounterIncrement()
        {
            SubTaskCounter++;
        }
    }
}
