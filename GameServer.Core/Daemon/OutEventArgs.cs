namespace GameServer.Core.Daemon
{
    public class OutEventArgs : EventArgs
    {
        public enum TargetStream
        {
            StandardIn,
            StandardOut,
            StandardError,
            None 
        }

        public OutEventArgs(string message, string targetStream, string execId, string scriptName, string type) { 
            Message = message;
            Target = (TargetStream) Enum.Parse(typeof(TargetStream), targetStream);
            ExecId = execId;
            ScriptName = scriptName;
            Type = type;
        }
        public string Message { get; }
        public string ExecId { get; }
        public string ScriptName { get; }
        public string Type { get; }
        public TargetStream Target{ get; } 
    }
}