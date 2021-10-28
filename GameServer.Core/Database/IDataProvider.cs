using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Core.Database
{
    public interface IDataProvider : IDisposable
    {
        void Connect();
        void Disconnect();
        T Read<T>(string Query) where T : DatabaseEntity;
        void Write<T>(string Query) where T : DatabaseEntity;
    }
}
