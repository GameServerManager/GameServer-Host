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

        public OutEventArgs(string message, string targetStream, string execID, string scriptName) { 
            Message = message;
            Target = (TargetStream) Enum.Parse(typeof(TargetStream), targetStream);
            ExecID = execID;
            ScriptName = scriptName;
        }
        public string Message { get; }
        public string ExecID { get; }
        public string ScriptName { get; }
        public TargetStream Target{ get; } 
    }
}