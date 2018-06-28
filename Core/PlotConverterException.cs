using System;

namespace Horego.BurstPlotConverter.Core
{
    internal class PlotConverterException : Exception
    {
        public PlotConverterException(string message) : base(message)
        {
        }
    }
}
