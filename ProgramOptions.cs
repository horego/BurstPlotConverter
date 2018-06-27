using CommandLine;

namespace Horego.BurstPlotConverter
{
    [Verb("inline", HelpText = "Inline plot conversion.")]
    partial class InlineFileOptions
    {
        [Option('r', "read", Required = true, HelpText = "Input file to be processed.")]
        public string InputFile { get; set; }

        [Option('m', "memory", Required = false, HelpText = "Memory in megabytes (1024 megabyte = 1 gigabyte).", Default = 0)]
        public int MemoryInMb { get; set; }

        [Option('c', "checkpoint", Required = false, HelpText = "Checkpoint information to resume plot conversion.", Default = 0L)]
        public long Checkpoint { get; set; }
    }

    [Verb("info", HelpText = "Plot and program information.")]
    class InfoOptions
    {
        [Option('r', "read", Required = true, HelpText = "Input file to be processed.")]
        public string InputFile { get; set; }

        [Option('m', "memory", Required = false, HelpText = "Memory in megabytes (1024 megabyte = 1 gigabyte).", Default = 0)]
        public int MemoryInMb { get; set; }
    }

    [Verb("outline", HelpText = "Sepeate output file plot conversion.")]
    partial class SeparateFileOptions
    {
        [Option('r', "read", Required = true, HelpText = "Input file to be processed.")]
        public string InputFile { get; set; }

        [Option('w', "write", Required = true, HelpText = "Output file.")]
        public string OutputFile { get; set; }

        [Option('m', "memory", Required = false, HelpText = "Memory in megabytes (1024 megabyte = 1 gigabyte).", Default = 0)]
        public int MemoryInMb { get; set; }

        [Option('c', "checkpoint", Required = false, HelpText = "Checkpoint information to resume plot conversion.", Default = 0L)]
        public long Checkpoint { get; set; }

    }

#if NET461
    partial class SeparateFileOptions
    {
        [Option('w', "watchprocess", Required = false, HelpText = "Watch the disk usage of an process. Do only conversion if the process have no read or write access", Default = null)]
        public string WatchProcess { get; set; }

        [Option('t', "threshold", Required = false, HelpText = "When watching a process the sum of read and write speed in megabyte per second (1024 kilobyte = 1 megabyte) of the process (named threshold) is used to autoresume and pause conversion.", Default = 10)]
        public float WatchProcessThresholdInMb { get; set; }
    }

    partial class InlineFileOptions
    {
        [Option('w', "watchprocess", Required = false, HelpText = "Watch the disk usage of an process. Do only conversion if the process have no read or write access", Default = null)]
        public string WatchProcess { get; set; }

        [Option('t', "threshold", Required = false, HelpText = "When watching a process the sum of read and write speed in megabyte per second (1024 kilobyte = 1 megabyte) of the process (named threshold) is used to autoresume and pause conversion.", Default = 10)]
        public float WatchProcessThresholdInMb { get; set; }
    }
#endif
}
