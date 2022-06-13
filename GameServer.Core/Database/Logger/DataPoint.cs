using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Database.Logger
{
    public class DataPoint
    {
        public DateTime Time { get; set; }
        public CpuStats? Cpu { get; set; }
        public DiskStats? Disk { get; set; }
        public MemoryStats? Ram { get; set; }
        public IDictionary<string, NetworkStats>? Networks { get; set; }
    }
}
