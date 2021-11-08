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
                switch (character)
                {
                    case '\\' when !escaped:
                        escaped = true;
                        break;
                    case '"' when !escaped:
                        ignoreSpace = !ignoreSpace;
                        break;
                    case ' ' when !ignoreSpace && !escaped:
                        bufferArgs.Add(buffer);
                        buffer = "";
                        break;
                    default:
                        buffer += character;
                        escaped = false;
                        break;
                }
            }

            bufferArgs.Add(buffer);
            Name = bufferArgs[0];

            Args.AddRange(bufferArgs.GetRange(1, bufferArgs.Count - 1));
        }
    }
}
