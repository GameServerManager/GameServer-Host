namespace GameServer.Core.Database.Logger
{
    public class NetworkStats
    {
        public ulong RxBytes;
        public ulong RxPackets;
        public ulong RxErrors;
        public ulong RxDropped;
        public ulong TxBytes;
        public ulong TxPackets;
        public ulong TxErrors;
        public ulong TxDropped;
    }
}