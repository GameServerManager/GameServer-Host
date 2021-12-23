using GameServer.Core.Daemon;

namespace GameServer.Worker
{
    internal class IOCache
    {
        private Dictionary<string, Dictionary<string, (string stderr, string stdout)>> Cache { get; } = new();

        internal void Add(string execID, string scriptName, OutEventArgs.TargetStream target, string message)
        {
            var containsScriptName = Cache.TryGetValue(scriptName, out var ScriptOut);
            if (!containsScriptName)
            {
                ScriptOut = new Dictionary<string, (string stderr, string stdout)>();
                Cache.Add(scriptName, ScriptOut);
            }
            var contiansExec = ScriptOut.TryGetValue(execID, out var ExecOut);

            if (!contiansExec)
            {
                ExecOut = ("","");
                ScriptOut.Add(execID, ExecOut);
            }

            if (target == OutEventArgs.TargetStream.StandardOut)
            {
                ExecOut.stdout += message;
            }
            else if (target == OutEventArgs.TargetStream.StandardError)
            {
                ExecOut.stderr += message;
            }
        }

        internal Dictionary<string, Dictionary<string, (string stderr, string stdout)>> GetAll()
        {
            return Cache;
        }
    }
}