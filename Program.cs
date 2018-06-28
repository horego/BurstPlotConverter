using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Horego.BurstPlotConverter.Core;
using Horego.BurstPlotConverter.Extensions;
using NLog;

namespace Horego.BurstPlotConverter
{
    class Program
    {
        private static Logger m_Log = LogManager.GetCurrentClassLogger();

        static int Main(string[] args)
        {
            IDisposable progressSubscription = null;
            PlotConverter plotConverter = null;
            
            void WriteProgess(ProgressEventArgs eventArgs)
            {
                m_Log.Info((eventArgs.IsPaused
                                      ? "[Paused]\t"
                                      : "") +
                                  $"{eventArgs.PercentComplete:0.00}%\telapsed: {eventArgs.ElapsedTime.ToReadableString()}\tremaining: {eventArgs.RemainingTime.ToReadableString()}.");
            }

            void WriteMemoryUsage(int usedMemoryInMb)
            {
                m_Log.Info($"Use {usedMemoryInMb} megabyte of memory.");
            }

            void WriteConversionInfo(string fileName)
            {
                m_Log.Info($"Start conversion of {fileName}.");
            }

            T TryExecute<T>(Func<T> execute)
            {
                try
                {
                    return execute();
                }
                catch (PlotConverterException e)
                {
                    m_Log.Error(e.Message);
                    return default(T);
                }
                catch (Exception e)
                {
                    m_Log.Error(e, "Unexpected error.");
                    return default(T);
                }
            }

#if NET461
            ProcessDiskPerformanceCounter diskPerformanceCounter = null;
            IDisposable diskPerformanceCounterSubscription = null;

            string GetInstanceNameOfProcess(string processName)
            {
                var processCategory = new ProcessPerformanceCounterCategory();
                var instanceNames = processCategory.GetInstanceNamesByProcessName(processName).ToList();

                if (instanceNames.Count == 0)
                {
                    throw new PlotConverterException($"No process instance with process name {processName} found.");
                }
                if (instanceNames.Count > 1)
                {
                    throw new PlotConverterException($"More then one process instance with process name {processName} found. It is only one process supported. Please close other processes.");
                }
                return instanceNames.First();
            }

            void ControlPauseAndResume(ProcessDiskPerformanceCounter.DiskUsageEventArgs usage, float bytePerSecondThreshold)
            {
                var diskUsage = usage.BytesReadPerSec + usage.BytesWritePerSec;
                if (diskUsage < bytePerSecondThreshold)
                {
                    if (plotConverter.Resume())
                        m_Log.Info($"Resume conversion. Disk usage {diskUsage.BytesToReadableString()} is below threshold {bytePerSecondThreshold.BytesToReadableString()} per second.");
                }
                else
                {
                    if (plotConverter.Pause())
                        m_Log.Info($"Pause conversion. Disk usage {diskUsage.BytesToReadableString()} is above threshold {bytePerSecondThreshold.BytesToReadableString()} per second.");
                }
            }

            void PauseAndResumeInfo(float bytePerSecondThreshold, string instanceName)
            {
                m_Log.Info($"Enabled resume and pause for process instance {instanceName}.{Environment.NewLine}" +
                                  $"Resume when disk usage is below {bytePerSecondThreshold.BytesToReadableString()}.{Environment.NewLine}" +
                                  $"Pause when disk usage is above {bytePerSecondThreshold.BytesToReadableString()}.");
            }

            void InitDiskPerformanceCounter(string watchProcess, float thresholdInBytes)
            {
                if (watchProcess == null)
                {
                    return;
                }
                diskPerformanceCounter = new ProcessDiskPerformanceCounter(GetInstanceNameOfProcess(watchProcess));
                diskPerformanceCounterSubscription = diskPerformanceCounter.DiskUsage.Subscribe(
                    i => ControlPauseAndResume(i, thresholdInBytes));
                PauseAndResumeInfo(thresholdInBytes, diskPerformanceCounter.InstanceName);
                diskPerformanceCounter.Start();
            }

            void DisposeDiskPerformanceCounter()
            {
                diskPerformanceCounterSubscription?.Dispose();
                diskPerformanceCounter?.Dispose();
            }
#endif

            var workingTask = Parser.Default.ParseArguments<InlineFileOptions, SeparateFileOptions, InfoOptions>(args)
                .MapResult(
                    (InlineFileOptions opts) =>
                    {
                        return TryExecute(() =>
                        {
                            plotConverter = new PlotConverter(new FileInfo(opts.InputFile), opts.MemoryInMb);
                            WriteMemoryUsage(plotConverter.UsedMemoryInMb);
                            WriteConversionInfo(opts.InputFile);
                            progressSubscription = plotConverter.Progress.Subscribe(WriteProgess);
#if NET461
                            InitDiskPerformanceCounter(opts.WatchProcess, opts.WatchProcessThresholdInMb * 1024 * 1024);
#endif
                            return plotConverter.RunInline(new PlotConverterCheckpoint(opts.Checkpoint));
                        });
                    },
                    (SeparateFileOptions opts) =>
                    {
                        return TryExecute(() => 
                        {
                            plotConverter = new PlotConverter(new FileInfo(opts.InputFile), opts.MemoryInMb);
                            WriteMemoryUsage(plotConverter.UsedMemoryInMb);
                            WriteConversionInfo(opts.InputFile);
                            progressSubscription = plotConverter.Progress.Subscribe(WriteProgess);
#if NET461
                            InitDiskPerformanceCounter(opts.WatchProcess, opts.WatchProcessThresholdInMb * 1024 * 1024);
#endif
                            return plotConverter.RunOutline(new FileInfo(opts.OutputFile), new PlotConverterCheckpoint(opts.Checkpoint));
                        });
                    },
                    (InfoOptions opts) =>
                    {
                        return TryExecute(() =>
                        {
                            plotConverter = new PlotConverter(new FileInfo(opts.InputFile), opts.MemoryInMb);
                            progressSubscription = plotConverter.Progress.Subscribe(WriteProgess);
                            return new TaskFactory().StartNew(() => m_Log.Info(plotConverter.Info()));
                        });
                    },
                    errs => new TaskFactory().StartNew(() => { }));

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                if (plotConverter == null)
                {
                    return;
                }
                m_Log.Info("Abort requested. Please wait until conversion has been safely aborted.");
                plotConverter.Abort();
                eventArgs.Cancel = true;
            };

            try
            {
                workingTask?.Wait();
            }
            catch (Exception e)
            {
                m_Log.Fatal(e);
                throw;
            }

            progressSubscription?.Dispose();
#if NET461
            DisposeDiskPerformanceCounter();
#endif

            if (plotConverter?.Checkpoint != null)
            {
                m_Log.Info($"Aborted conversion. You can safely resume conversion with:{Environment.NewLine}" +
                                    $"-c {plotConverter.Checkpoint.Position}{Environment.NewLine}" +
                                    $"Press enter to exit.");
                Console.ReadLine();
            }
            return 0;
        }
    }
}
