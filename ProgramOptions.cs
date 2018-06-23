using CommandLine;

namespace Horego.BurstPlotConverter
{
    [Verb("inline", HelpText = "Inline plot conversion.")]
    class InlineFileOptions
    {
        [Option('r', "read", Required = true, HelpText = "Input file to be processed.")]
        public string InputFile { get; set; }

        [Option('m', "memory", Required = false, HelpText = "Memory in megabytes (1024 megabyte = 1 gigabyte).", Default = 0)]
        public int MemoryInMb { get; set; }
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
    class SeparateFileOptions
    {
        [Option('r', "read", Required = true, HelpText = "Input file to be processed.")]
        public string InputFile { get; set; }

        [Option('w', "write", Required = true, HelpText = "Output file.")]
        public string OutputFile { get; set; }

        [Option('m', "memory", Required = false, HelpText = "Memory in megabytes (1024 megabyte = 1 gigabyte).", Default = 0)]
        public int MemoryInMb { get; set; }
    }
}
