using GameServer.Core.Daemon;

namespace GameServer.Worker
{
    internal class IOCache
    {
        private Dictionary<string, Dictionary<string, (string stderr, string stdout)>> Cache { get; } = new();

        internal void Add(string execID, string scriptName, OutEventArgs.TargetStream target, string message)
        {
            var containsScriptName = Cache.ContainsKey(scriptName);
            if (!containsScriptName)
            {
                Cache.Add(scriptName, new Dictionary<string, (string stderr, string stdout)>());
            }


            if (!Cache[scriptName].ContainsKey(execID))
            {
                Cache[scriptName].Add(execID, ("", ""));
            }

            if (target == OutEventArgs.TargetStream.StandardOut)
            {
                Cache[scriptName][execID] = new (Cache[scriptName][execID].stderr, Cache[scriptName][execID].stdout + message);
            }
            else if (target == OutEventArgs.TargetStream.StandardError)
            {
                Cache[scriptName][execID] = new (Cache[scriptName][execID].stderr + message, Cache[scriptName][execID].stdout);
            }
        }

        internal Dictionary<string, Dictionary<string, (string stderr, string stdout)>> GetAll()
        {
            return Cache;
        }
    }
}