namespace Horego.BurstPlotConverter.Extensions
{
    internal static class ByteExtensions
    {
        private const long KB = 1024, MB = KB * 1024, GB = MB * 1024, TB = GB * 1024;

        public static string BytesToReadableString(this float bytes)
        {
            double size = bytes;
            var suffix = "B";

            if (bytes >= TB)
            {
                size = bytes / TB;
                suffix = "TB";
            }
            else if (bytes >= GB)
            {
                size = bytes / GB;
                suffix = "GB";
            }
            else if (bytes >= MB)
            {
                size = bytes / MB;
                suffix = "MB";
            }
            else if (bytes >= KB)
            {
                size = bytes / KB;
                suffix = "KB";
            }

            return $"{size:0.00} {suffix}";
        }
    }
}
