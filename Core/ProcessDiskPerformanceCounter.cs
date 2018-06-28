using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using Horego.BurstPlotConverter.Extensions;

#if NET461

namespace Horego.BurstPlotConverter.Core
{
    internal class ProcessDiskPerformanceCounter : IDisposable
    {
        public string InstanceName { get; }
        private readonly TimeSpan m_CheckIntervall = TimeSpan.FromSeconds(1);
        private readonly ProcessDiskReadBytesPerformanceCounter m_ReadSpeedCounter;
        private readonly ProcessDiskWriteBytesPerformanceCounter m_WriteSpeedCounter;
        public Subject<DiskUsageEventArgs> DiskUsage { get; }
        private IDisposable m_TimerSubscription;

        public ProcessDiskPerformanceCounter(string instanceName)
        {
            InstanceName = instanceName;
            m_ReadSpeedCounter = new ProcessDiskReadBytesPerformanceCounter(instanceName);
            m_WriteSpeedCounter = new ProcessDiskWriteBytesPerformanceCounter(instanceName);
            DiskUsage = new Subject<DiskUsageEventArgs>();
        }

        public void Start()
        {
            m_TimerSubscription = Observable.Timer(TimeSpan.Zero, m_CheckIntervall).Subscribe(l => Check());
        }

        void Check()
        {
            var readSpeed = m_ReadSpeedCounter.Read();
            var writeSpeed = m_WriteSpeedCounter.Read();
            DiskUsage.OnNext(new DiskUsageEventArgs(readSpeed, writeSpeed));
        }

        internal class DiskUsageEventArgs
        {
            public DiskUsageEventArgs(float bytesReadPerSec, float bytesWritePerSec)
            {
                BytesReadPerSec = bytesReadPerSec;
                BytesWritePerSec = bytesWritePerSec;
            }

            public float BytesReadPerSec { get; }
            public float BytesWritePerSec { get; }

            public override string ToString()
            {
                return $"read {BytesReadPerSec.BytesToReadableString()}, write {BytesWritePerSec.BytesToReadableString()} per sec.";
            }
        }

        public void Dispose()
        {
            m_ReadSpeedCounter?.Dispose();
            m_WriteSpeedCounter?.Dispose();
            m_TimerSubscription?.Dispose();
        }
    }

    internal class ProcessDiskReadBytesPerformanceCounter : IDisposable
    {
        private readonly string m_InstanceName;
        private readonly PerformanceCounter m_PerformanceCounter;

        public ProcessDiskReadBytesPerformanceCounter(string instanceName)
        {
            m_InstanceName = instanceName;
            m_PerformanceCounter = new PerformanceCounter("Process", "io read bytes/sec", instanceName, true);
        }

        public float Read()
        {
            return m_PerformanceCounter.NextValue();
        }

        public void Dispose()
        {
            m_PerformanceCounter?.Dispose();
        }
    }

    internal class ProcessDiskWriteBytesPerformanceCounter : IDisposable
    {
        private readonly string m_InstanceName;
        private readonly PerformanceCounter m_PerformanceCounter;

        public ProcessDiskWriteBytesPerformanceCounter(string instanceName)
        {
            m_InstanceName = instanceName;
            m_PerformanceCounter = new PerformanceCounter("Process", "io write bytes/sec", instanceName, true);
        }

        public float Read()
        {
            return m_PerformanceCounter.NextValue();
        }

        public void Dispose()
        {
            m_PerformanceCounter?.Dispose();
        }
    }

    internal class ProcessPerformanceCounterCategory
    {
        private readonly PerformanceCounterCategory m_PerformanceCounterCategory;

        public ProcessPerformanceCounterCategory()
        {
            m_PerformanceCounterCategory = new PerformanceCounterCategory("Process");
        }

        public IEnumerable<string> GetInstanceNamesByProcessName(string processName, StringComparison comparison = StringComparison.CurrentCultureIgnoreCase)
        {
            var instanceNames = m_PerformanceCounterCategory.GetInstanceNames();
            var foundInstances = instanceNames.Where(i => string.Equals(GetProcessName(i), processName, comparison));
            return foundInstances;
        }

        static string GetProcessName(string instaneName)
        {
            var regex = new Regex("(^)(?<processName>.*)(#)(?<instanceNr>[0-9]+$)");
            var match = regex.Match(instaneName);
            if (match.Success)
            {
                return match.Groups["processName"].Value;
            }
            return instaneName;
        }
    }
}

#endif