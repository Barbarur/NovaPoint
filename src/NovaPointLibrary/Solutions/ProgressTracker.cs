using CamlBuilder;
using Microsoft.Extensions.Logging;
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
        internal readonly NPLogger _logger;

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

        internal ProgressTracker(NPLogger logger, int totalCount)
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
            float progressValue = (float)Math.Round(_counter * _counterStep, 2);
            ProgressUpdateReport(progressValue);
        }

        internal void ProgressUpdateReport()
        {
            _counter++;
            double progressValue = Math.Round(_counter * _counterStep, 2);
            
            ProgressUpdateReport(progressValue);
        }
        
        private void ProgressUpdateReport(double progressvalue)
        {
            if (_parentProgress == null)
            {
                double progress = Math.Round(progressvalue * 100, 2);

                _logger.ProgressUI(progress);
            }
            else
            {
                _parentProgress.ProgressUpdateFromChild(progressvalue);
            }
        }

        private void ProgressUpdateFromChild(double childProgressvalue)
        {
            double progressValue = Math.Round((_counter + childProgressvalue) * _counterStep, 2);
            
            ProgressUpdateReport(progressValue);
        }
    }
}
