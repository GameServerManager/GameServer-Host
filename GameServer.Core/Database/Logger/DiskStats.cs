namespace GameServer.Core.Database.Logger
{
    public class DiskStats
    {
        public ulong ReadCountNormalized { get; set; }
        public ulong ReadSizeBytes { get; set; }
        public ulong WriteCountNormalized { get; set; }
        public ulong WriteSizeBytes { get; set; }

    }
}