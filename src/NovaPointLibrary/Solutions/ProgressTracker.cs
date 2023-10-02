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
        internal readonly LogHelper _logHelper;
        private float _counter;
        private int _totalUnits;
        private int Total
        {
            get { return _totalUnits; }
            set
            {
                _totalUnits = value;
                if(value > 0 ) { _counterStep = 1 / value; }
            }
        }
        private float _counterStep = 0;

        private float SubTaskCounter = 0;
        private float SubTotalCount = 1;
        
        internal ProgressTracker(LogHelper logHelper, int totalCount)
        {
            _logHelper = logHelper;
            _counter = 0;
            Total = totalCount;

        }

        internal void MainReportProgress(string msg)
        {
            double progress = Math.Round(_counter * 100 / Total, 2);
            _logHelper.AddProgressToUI(progress);
            _logHelper.AddLogToUI(msg);
        }

        internal void MainCounterIncrement()
        {
            double progress = Math.Round(_counter * 100 / Total, 2);
            _logHelper.AddProgressToUI(progress);
            _counter++;
        }

        internal void SubTaskProgressReset(float subTaskCount)
        {
            SubTotalCount = 1 + subTaskCount;
            SubTaskCounter = 0;
        }

        internal void SubTaskReportProgress(string msg)
        {
            var progress = Math.Round( ((_counter / Total) + ( SubTaskCounter * _counterStep / SubTotalCount)) * 100, 2);
            _logHelper.AddProgressToUI(progress);
            _logHelper.AddLogToUI(msg);
        }

        internal void SubTaskCounterIncrement()
        {
            var progress = Math.Round(((_counter / Total) + (SubTaskCounter * _counterStep / SubTotalCount)) * 100, 2);
            _logHelper.AddProgressToUI(progress);
            SubTaskCounter++;
        }
    }
}
