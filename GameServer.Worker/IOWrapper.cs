using Docker.DotNet;
using System.Diagnostics.CodeAnalysis;
using System.Timers;

namespace GameServer.Worker
{
    internal class IOWrapper
    {
        private Dictionary<string, MultiplexedStream> StdIn = new Dictionary<string, MultiplexedStream>();

        private object Iolock = new();
        private Dictionary<string, string> IoOut { get; } = new Dictionary<string, string>();
        private System.Timers.Timer myTimer = new System.Timers.Timer();


        public delegate void UpdatedIOHandler(object sender, IOEventArgs e);
        public event UpdatedIOHandler UpdatedIO;

        public IOWrapper()
        {
            myTimer.Elapsed += FlushAll;
            myTimer.Interval = 250; // 1000 ms is one second
            myTimer.Start();
        }

        private void FlushAll(object sender, ElapsedEventArgs e)
        {
            lock (Iolock)
            {
                foreach (var item in IoOut)
                {
                    UpdatedIO.Invoke(this, new IOEventArgs(item.Value, "StandardOut", item.Key, "message"));
                    IoOut[item.Key] = "";
                }
            }
        }

        internal bool TryGetValue(string execId, [MaybeNullWhen(false)] out MultiplexedStream stream)
        {
            return StdIn.TryGetValue(execId, out stream);
        }

        internal Task Add(string iD, MultiplexedStream stream)
        {
            return Task.Run(async () =>
            {

                StdIn.Add(iD, stream);

                var token = new CancellationTokenSource();
                var buffer = new byte[100];
                MultiplexedStream.ReadResult res;
                lock (Iolock)
                {
                    IoOut.Add(iD, "");
                }
                do
                {
                    Array.Clear(buffer, 0, buffer.Length);
                    res = await stream.ReadOutputAsync(buffer, 0, 100, token.Token);
                    if (res.Count != 0)
                    {
                        lock (Iolock)
                        {
                            IoOut[iD] += System.Text.Encoding.Default.GetString(buffer);
                        }
                    }
                } while (!res.EOF);
            });
        }

        internal Task Remove(string iD)
        {
            return Task.Run(() => {
                myTimer.Start();
                FlushAll(this, null);
                StdIn.Remove(iD);
                UpdatedIO.Invoke(this, new IOEventArgs("", "None", iD, "closed"));
            });
        }

        internal Task RemoveAll()
        {
            List<Task> taskPool = new();
            foreach(var id in StdIn.Keys){
                taskPool.Add(Remove(id));
            }
            return Task.WhenAll(taskPool.ToArray());
        }
    }
}