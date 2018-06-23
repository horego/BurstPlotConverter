using System;
using System.IO;

namespace Horego.BurstPlotConverter
{
    internal class PlotFile
    {
        private readonly FileInfo m_File;

        public ulong Id { get; private set; }
        public long Offset { get; private set; }
        public long Nonces { get; private set; }
        public long Stagger { get; private set; }
        public long RealPlotSize => m_File.Length;
        public long ExpectedPlotSize => Nonces * Constants.NONCE_SIZE;
        public string Poc1FileName => $"{Id}_{Offset}_{Nonces}_{Stagger}";
        public string Poc2FileName => $"{Id}_{Offset}_{Nonces}";

        public PlotFile(FileInfo file)
        {
            m_File = file ?? throw new ArgumentNullException(nameof(file));
            ParsePlotName(m_File.Name);
        }

        void ParsePlotName(string plotName)
        {
            var parts = plotName.Split(new[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4)
                throw new InvalidOperationException($"Plot {plotName} is not in valid Poc1 plotfile format.");

            Id = ulong.Parse(parts[0]);
            Offset = long.Parse(parts[1]);
            Nonces = long.Parse(parts[2]);
            Stagger = long.Parse(parts[3]);
        }

        public void Rename(string toName)
        {
            m_File.MoveTo(Path.Combine(m_File.Directory.FullName, toName));
        }

        public void Validate()
        {
            if (Nonces != Stagger)
                throw new InvalidOperationException($"Plot file {m_File.Name} isnt optimized.");

            if (RealPlotSize != ExpectedPlotSize)
                throw new InvalidOperationException($"The real size ({RealPlotSize}) is not what we expected ({ExpectedPlotSize}).");
        }
    }
}