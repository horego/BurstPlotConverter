using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;

namespace Horego.BurstPlotConverter
{
    class Program
    {
        static int Main(string[] args)
        {

#if NET461
            string GetInstanceNameOfProcess(string processName)
            {
                var processCategory = new ProcessPerformanceCounterCategory();
                var instanceNames = processCategory.GetInstanceNamesByProcessName(processName).ToList();

                if (instanceNames.Count == 0)
                {
                    throw new InvalidOperationException($"No process instance with process name {processName} found.");
                }
                if (instanceNames.Count > 1)
                {
                    throw new InvalidOperationException($"More then one process instance with process name {processName} found. It is only one process supported. Please close other processes.");
                }
                return instanceNames.First();
            }
#endif

            IDisposable progressSubscription = null;
            PlotConverter plotConverter = null;

            void WriteProgess(ProgressEventArgs eventArgs)
            {
                Console.WriteLine((eventArgs.IsPaused
                                      ? "[Paused]\t"
                                      : "") +
                                  $"{eventArgs.PercentComplete:0.00}%\telapsed: {eventArgs.ElapsedTime.ToReadableString()}\tremaining: {eventArgs.RemainingTime.ToReadableString()}.");
            }

            void WriteMemoryUsage(int usedMemoryInMb)
            {
                Console.WriteLine($"Use {usedMemoryInMb} megabyte of memory.");
            }

#if NET461
            ProcessDiskPerformanceCounter diskPerformanceCounter = null;
            IDisposable diskPerformanceCounterSubscription = null;

            void ControlPauseAndResume(ProcessDiskPerformanceCounter.DiskUsageEventArgs usage, float bytePerSecondThreshold)
            {
                var diskUsage = usage.BytesReadPerSec + usage.BytesWritePerSec;
                if (diskUsage < bytePerSecondThreshold)
                {
                    if (plotConverter.Resume())
                        Console.WriteLine($"Resume conversion. Disk usage {diskUsage.BytesToReadableString()} is below threshold {bytePerSecondThreshold.BytesToReadableString()} per second.");
                }
                else
                {
                    if (plotConverter.Pause())
                        Console.WriteLine($"Pause conversion. Disk usage {diskUsage.BytesToReadableString()} is above threshold {bytePerSecondThreshold.BytesToReadableString()} per second.");
                }
            }

            void PauseAndResumeInfo(float bytePerSecondThreshold, string instanceName)
            {
                Console.WriteLine($"Enabled resume and pause for process instance {instanceName}.{Environment.NewLine}" +
                                  $"Resume when disk usage is below {bytePerSecondThreshold.BytesToReadableString()}.{Environment.NewLine}" +
                                  $"Pause when disk usage is above {bytePerSecondThreshold.BytesToReadableString()}.");

            }
#endif

            var workingTask = Parser.Default.ParseArguments<InlineFileOptions, SeparateFileOptions, InfoOptions>(args)
                .MapResult(
                    (InlineFileOptions opts) =>
                    {
                        plotConverter = new PlotConverter(new FileInfo(opts.InputFile), opts.MemoryInMb);
                        WriteMemoryUsage(plotConverter.UsedMemoryInMb);
                        progressSubscription = plotConverter.Progress.Subscribe(WriteProgess);
#if NET461
                        if (opts.WatchProcess != null)
                        {
                            var threshold = opts.WatchProcessThresholdInMb * 1024 * 1024;
                            diskPerformanceCounter = new ProcessDiskPerformanceCounter(GetInstanceNameOfProcess(opts.WatchProcess));
                            diskPerformanceCounterSubscription = diskPerformanceCounter.DiskUsage.Subscribe(
                                i => ControlPauseAndResume(i, threshold));
                            PauseAndResumeInfo(threshold, diskPerformanceCounter.InstanceName);
                            diskPerformanceCounter.Start();
                        }
#endif
                        return plotConverter.RunInline(new PlotConverterCheckpoint(opts.Checkpoint));
                    },
                    (SeparateFileOptions opts) =>
                    {
                        plotConverter = new PlotConverter(new FileInfo(opts.InputFile), opts.MemoryInMb);
                        WriteMemoryUsage(plotConverter.UsedMemoryInMb);
                        progressSubscription = plotConverter.Progress.Subscribe(WriteProgess);
#if NET461
                        if (opts.WatchProcess != null)
                        {
                            var threshold = opts.WatchProcessThresholdInMb * 1024 * 1024;
                            diskPerformanceCounter = new ProcessDiskPerformanceCounter(GetInstanceNameOfProcess(opts.WatchProcess));
                            diskPerformanceCounterSubscription = diskPerformanceCounter.DiskUsage.Subscribe(
                                i => ControlPauseAndResume(i, threshold));
                            PauseAndResumeInfo(threshold, diskPerformanceCounter.InstanceName);
                            diskPerformanceCounter.Start();
                        }
#endif
                        return plotConverter.RunOutline(new FileInfo(opts.OutputFile), new PlotConverterCheckpoint(opts.Checkpoint));
                    },
                    (InfoOptions opts) =>
                    {
                        plotConverter = new PlotConverter(new FileInfo(opts.InputFile), opts.MemoryInMb);
                        progressSubscription = plotConverter.Progress.Subscribe(WriteProgess);
                        return new TaskFactory().StartNew(() => plotConverter.Info());
                    },
                    errs => new TaskFactory().StartNew(() => { }));

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                if (plotConverter == null)
                {
                    return;
                }
                Console.WriteLine("Abort requested. Please wait until conversion has been safely aborted.");
                plotConverter.Abort();
                eventArgs.Cancel = true;
            };

            workingTask.Wait();
            progressSubscription?.Dispose();
#if NET461
            diskPerformanceCounterSubscription?.Dispose();
            diskPerformanceCounter?.Dispose();
#endif

            if (plotConverter?.Checkpoint != null)
            {
                Console.WriteLine($"Aborted conversion. You can safely resume conversion with:{Environment.NewLine}" +
                                    $"-c {plotConverter.Checkpoint.Position}{Environment.NewLine}" +
                                    $"Press enter to exit.");
                Console.ReadLine();
            }

            return 0;
        }
    }
}
