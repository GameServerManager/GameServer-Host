// See https://aka.ms/new-console-template for more information
using GameServer.Core.Command;
using GameServer.Core.Settings;
using GameServer.Main;

main();

void main()
{
    var queue = new CommandQueue();

    ServerWorker serverWorker = new(queue, GameServerSettings.FromFile(@".\config.yml"));

    Thread t = new(() => serverWorker.Start());

    t.Start();
    do
    {
        try
        {
            queue.PushCommand(Console.ReadLine());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    } while (serverWorker.Running);

    t.Join();
}