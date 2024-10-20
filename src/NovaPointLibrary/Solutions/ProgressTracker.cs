using NovaPointLibrary.Core.Logging;

namespace NovaPointLibrary.Solutions
{
    internal class ProgressTracker
    {
        internal readonly LoggerSolution _logger;

        private int _counter;
        private int _totalUnits;
        private int Total
        {
            get { return _totalUnits; }
            set
            {
                _totalUnits = value;
                if (value > 0)
                { 
                    _counterStep = 1 / (double)value;
                }
            }
        }

        private double _counterStep = 0;
        private readonly ProgressTracker? _parentProgress = null;

        internal ProgressTracker(LoggerSolution logger, int totalCount)
        {
            _logger = logger;
            _counter = 0;
            Total = totalCount;
        }

        internal ProgressTracker(ProgressTracker parentProgress, int totalCount)
        {
            _logger = parentProgress._logger;
            _parentProgress = parentProgress;
            _counter = 0;
            Total = totalCount;
        }

        internal void IncreaseTotalCount(int addUnits)
        {
            Total += addUnits;
            double progressValue = Math.Round(_counter * _counterStep, 4);
            ProgressUpdateReport(progressValue);
        }

        internal void ProgressUpdateReport()
        {
            _counter++;
            double progressValue = Math.Round(_counter * _counterStep, 4);

            ProgressUpdateReport(progressValue);
        }
        
        private void ProgressUpdateReport(double progressValue)
        {
            if (_parentProgress == null)
            {
                double progress = Math.Round(progressValue * 100, 2);

                _logger.Progress(progress);
            }
            else
            {
                _parentProgress.ProgressUpdateFromChild(progressValue);
            }
        }

        private void ProgressUpdateFromChild(double childProgressvalue)
        {
            double progressValue = Math.Round((_counter + childProgressvalue) * _counterStep, 4);

            ProgressUpdateReport(progressValue);
        }
    }
}
