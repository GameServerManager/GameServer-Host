using System.Threading.Tasks.Dataflow;

namespace GameServer.Core.Command
{
    public class SampleEventArgs
    {
        public SampleEventArgs(string command) { Command = command; }
        public string Command { get; } // readonly
    }

    public class CommandQueue : IDisposable
    {
        private ActionBlock<SampleEventArgs> queue;
        public bool IsEmpty
        {
            get
            {
                return queue.InputCount == 0;
            }
        }
        public delegate void NewCommandHandler(object sender, SampleEventArgs e);

        // Declare the event.
        public event NewCommandHandler NewCommand;

        public CommandQueue()
        {
            this.queue = new ActionBlock<SampleEventArgs>(item => NewCommand?.Invoke(this, item));
        }

        public virtual void PushCommand(string command)
        {
            queue.Post(new SampleEventArgs(command));
        }

        public void Dispose()
        {
            queue.Completion.Wait();
        }
    }
}
