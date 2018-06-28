namespace Horego.BurstPlotConverter.Core
{
    internal static class Constants
    {
        public const int SCOOPS_IN_NONCE = 4096;
        public const int SHABAL256_HASH_SIZE = 32;
        public const int SCOOP_SIZE = SHABAL256_HASH_SIZE * 2;
        public const int NONCE_SIZE = SCOOP_SIZE * SCOOPS_IN_NONCE;
    }
}
