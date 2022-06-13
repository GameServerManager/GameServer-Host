using System.Threading.Tasks.Dataflow;

namespace GameServer.Core.Command
{
    public class NewCommandEventArgs
    {
        public NewCommandEventArgs(string command) { Command = command; }
        public string Command { get; } 
    }

    public class CommandQueue : IDisposable
    {
        public delegate void NewCommandHandler(object sender, NewCommandEventArgs e);
        public event NewCommandHandler? NewCommand;
        private readonly ActionBlock<NewCommandEventArgs> _queue;
        public bool IsEmpty => _queue.InputCount == 0;

        public CommandQueue()
        {
            _queue = new ActionBlock<NewCommandEventArgs>(item => NewCommand?.Invoke(this, item));
        }

        public virtual void PushCommand(string command)
        {
            _queue.Post(new NewCommandEventArgs(command));
        }

        public void Dispose()
        {
            _queue.Completion.Wait();
        }
    }
}
