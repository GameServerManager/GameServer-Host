namespace GameServer.Core.Daemon
{
    public class OutEventArgs : EventArgs
    {
        public enum TargetStream
        {
            StandardIn,
            StandardOut,
            StandardError
        }

        public OutEventArgs(string message, string targetStream) { 
            Message = message;
            Target = (TargetStream) Enum.Parse(typeof(TargetStream), targetStream);
        }
        public string Message { get; } // readonly
        public TargetStream Target{ get; } // readonly
    }
}