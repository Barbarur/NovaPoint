using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaPointLibrary.Solutions
{
    internal class SolutionProgressTracker
    {
        internal readonly Main _main;
        private float _counter;
        private int _totalUnits;
        private int Total
        {
            get { return _totalUnits; }
            set
            {
                _totalUnits = value;
                if (value > 0) { _counterStep = 1 / value; }
            }
        }
        private float _counterStep = 0;
        private readonly SolutionProgressTracker? _parentProgress = null;


        private float SubTaskCounter = 0;
        private float SubTotalCount = 1;

        internal SolutionProgressTracker(Main main, int totalCount)
        {
            _main = main;
            _counter = 0;
            Total = totalCount;

        }

        internal SolutionProgressTracker(SolutionProgressTracker parentProgress, int totalCount)
        {
            _main = parentProgress._main;
            _parentProgress = parentProgress;
            _counter = 0;
            Total = totalCount;
        }

        internal void IncreaseTotalCount(int addUnits)
        {
            Total += addUnits;
            ProgressUpdateReport(_counter);
        }

        internal void ProgressUpdateReport()
        {
            _counter++;
            ProgressUpdateReport(_counter);
        }
        
        private void ProgressUpdateReport(double counter)
        {
            if (_parentProgress == null)
            {
                double progress = Math.Round(counter * 100 / Total, 2);
                _main.AddProgressToUI(progress);
            }
            else
            {
                double progress = Math.Round(counter / Total, 2);
                _parentProgress.ProgressUpdateFromChild(progress);
            }
        }

        private void ProgressUpdateFromChild(double childProgress)
        {
            var counter = _counter + _counterStep * childProgress;
            ProgressUpdateReport(counter);
        }
    }
}
