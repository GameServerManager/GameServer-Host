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

        public OutEventArgs(string message, string targetStream, string execID, string scriptName, string type) { 
            Message = message;
            Target = (TargetStream) Enum.Parse(typeof(TargetStream), targetStream);
            ExecID = execID;
            ScriptName = scriptName;
            Type = type;
        }
        public string Message { get; }
        public string ExecID { get; }
        public string ScriptName { get; }
        public string Type { get; }
        public TargetStream Target{ get; } 
    }
}