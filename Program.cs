using System;
using System.IO;
using CommandLine;

namespace Horego.BurstPlotConverter
{
    class Program
    {
        static int Main(string[] args)
        {
            IDisposable progressSubscription = null;

            void WriteProgess(ProgressEventArgs eventArgs)
            {
                Console.WriteLine($"{eventArgs.PercentComplete:0.00}%\telapsed: {eventArgs.ElapsedTime.ToReadableString()}\tremaining: {eventArgs.RemainingTime.ToReadableString()}.");
            }

            void WriteMemoryUsage(int usedMemoryInMb)
            {
                Console.WriteLine($"Use {usedMemoryInMb} megabyte of memory.");
            }

            var returnCode = Parser.Default.ParseArguments<InlineFileOptions, SeparateFileOptions, InfoOptions>(args)
                .MapResult(
                    (InlineFileOptions opts) =>
                    {
                        var plotConverter = new PlotConverter(new FileInfo(opts.InputFile), opts.MemoryInMb);
                        WriteMemoryUsage(plotConverter.UsedMemoryInMb);
                        progressSubscription = plotConverter.Progress.Subscribe(WriteProgess);
                        plotConverter.RunInline().Wait();
                        return 0;
                    },
                    (SeparateFileOptions opts) =>
                    {
                        var plotConverter = new PlotConverter(new FileInfo(opts.InputFile), opts.MemoryInMb);
                        WriteMemoryUsage(plotConverter.UsedMemoryInMb);
                        progressSubscription = plotConverter.Progress.Subscribe(WriteProgess);
                        plotConverter.RunOutline(new FileInfo(opts.OutputFile)).Wait();
                        return 0;
                    },
                    (InfoOptions opts) =>
                    {
                        var plotConverter = new PlotConverter(new FileInfo(opts.InputFile), opts.MemoryInMb);
                        progressSubscription = plotConverter.Progress.Subscribe(WriteProgess);
                        Console.Write(plotConverter.Info());
                        return 0;
                    },
                    errs => 1);

            progressSubscription?.Dispose();
            return returnCode;
        }
    }
}
