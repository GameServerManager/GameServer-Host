using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Database
{
    public class DatabaseLock
    {
        private readonly object toLock;

        public DatabaseLock(object toLock)
        {
            this.toLock = toLock;
        }

        public LockReleaser Lock(TimeSpan timeout)
        {
            if (Monitor.TryEnter(toLock, timeout))
            {
                return new LockReleaser(toLock);
            }
            throw new TimeoutException();
        }

        public struct LockReleaser : IDisposable
        {
            private readonly object toRelease;

            public LockReleaser(object toRelease)
            {
                this.toRelease = toRelease;
            }

            public void Dispose()
            {
                Monitor.Exit(toRelease);
            }
        }
    }
}
