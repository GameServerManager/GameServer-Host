using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Database
{
    public interface IDatabaseProvider
    {
        void Connect();
        void Disconnect();
    }
}
