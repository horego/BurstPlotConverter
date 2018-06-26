using System;

namespace Horego.BurstPlotConverter
{
    internal class ProgressEventArgs
    {
        public TimeSpan ElapsedTime { get; }
        public TimeSpan RemainingTime { get; }
        public double PercentComplete { get; }
        public bool IsPaused { get; }

        public ProgressEventArgs(TimeSpan elapsedTime, TimeSpan remainingTime, double percentComplete, bool isPaused)
        {
            ElapsedTime = elapsedTime;
            RemainingTime = remainingTime;
            PercentComplete = percentComplete;
            IsPaused = isPaused;
        }
    }
}