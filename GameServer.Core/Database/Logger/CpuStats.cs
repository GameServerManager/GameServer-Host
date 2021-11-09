namespace GameServer.Core.Database.Logger
{
    public class CpuStats
    {
        public ulong CpuDelta { get; set; }
        public ulong SystemCpuDelta { get; set; }
        public ulong NumberCpus { get; set; }
        public double CpuUsage { get => (CpuDelta / SystemCpuDelta) * NumberCpus * 100.0; }
    }
}