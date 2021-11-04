namespace GameServer.Core.Command
{
    public class Command
    {
        public string Name { get; }
        public List<string> Args { get; } = new List<string>();

        public Command(string command)
        {
            bool escaped = false;
            bool ignoreSpace = false;
            string buffer = "";
            List<string> bufferArgs = new();

            foreach (var character in command)
            {
                if (character == '\\' && !escaped)
                    escaped = true;
                else if (character == '"' && !escaped)
                    ignoreSpace = !ignoreSpace;
                else if (character == ' ' && !ignoreSpace && !escaped)
                {
                    bufferArgs.Add(buffer);
                    buffer = "";
                }
                else
                {
                    buffer += character;
                    escaped = false;
                }
            }

            bufferArgs.Add(buffer);
            Name = bufferArgs[0];

            Args.AddRange(bufferArgs.GetRange(1, bufferArgs.Count - 1));
        }
    }
}
