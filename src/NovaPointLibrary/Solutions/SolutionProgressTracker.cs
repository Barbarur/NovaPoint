using CamlBuilder;
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
        private readonly SolutionProgressTracker? _parentProgress = null;


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
                
                _main.AddProgressToUI(progress);
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
