namespace GameServer.Worker
{
    internal class IOEventArgs
    {

        public IOEventArgs(string message, string targetStream, string execID, string type)
        {
            Message = message;
            Target = targetStream;
            ExecID = execID;
            Type = type;
        }
        public string Message { get; }
        public string ExecID { get; }
        public string ScriptName { get; }
        public string Type { get; }
        public string Target { get; }
    }
}