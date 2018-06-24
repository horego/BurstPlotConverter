using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;

namespace Horego.BurstPlotConverter
{
    class Program
    {
        static int Main(string[] args)
        {
            IDisposable progressSubscription = null;
            PlotConverter plotConverter = null;

            void WriteProgess(ProgressEventArgs eventArgs)
            {
                Console.WriteLine($"{eventArgs.PercentComplete:0.00}%\telapsed: {eventArgs.ElapsedTime.ToReadableString()}\tremaining: {eventArgs.RemainingTime.ToReadableString()}.");
            }

            void WriteMemoryUsage(int usedMemoryInMb)
            {
                Console.WriteLine($"Use {usedMemoryInMb} megabyte of memory.");
            }

            var workingTask = Parser.Default.ParseArguments<InlineFileOptions, SeparateFileOptions, InfoOptions>(args)
                .MapResult(
                    (InlineFileOptions opts) =>
                    {
                        plotConverter = new PlotConverter(new FileInfo(opts.InputFile), opts.MemoryInMb);
                        WriteMemoryUsage(plotConverter.UsedMemoryInMb);
                        progressSubscription = plotConverter.Progress.Subscribe(WriteProgess);
                        return plotConverter.RunInline(new PlotConverterCheckpoint(opts.Checkpoint));
                    },
                    (SeparateFileOptions opts) =>
                    {
                        plotConverter = new PlotConverter(new FileInfo(opts.InputFile), opts.MemoryInMb);
                        WriteMemoryUsage(plotConverter.UsedMemoryInMb);
                        progressSubscription = plotConverter.Progress.Subscribe(WriteProgess);
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
