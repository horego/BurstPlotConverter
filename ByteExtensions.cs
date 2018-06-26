using System;

namespace Horego.BurstPlotConverter
{
    internal static class ByteExtensions
    {
        public static string BytesToReadableString(this float bytes, int precission = 2)
        {
            long B = 0, KB = 1024, MB = KB * 1024, GB = MB * 1024, TB = GB * 1024;
            double size = bytes;
            var suffix = nameof(B);

            if (bytes >= TB)
            {
                size = Math.Round(bytes / TB, precission);
                suffix = "TB";
            }
            else if (bytes >= GB)
            {
                size = Math.Round(bytes / GB, precission);
                suffix = "GB";
            }
            else if (bytes >= MB)
            {
                size = Math.Round(bytes / MB, precission);
                suffix = "MB";
            }
            else if (bytes >= KB)
            {
                size = Math.Round(bytes / KB, precission);
                suffix = "KB";
            }

            return $"{size} {suffix}";
        }
    }
}
