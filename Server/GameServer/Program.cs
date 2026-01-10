using System;
using FreeNet;

namespace GameServer
{
    class Program
    {
        const int MAX_CONNECTION = 10000;
        const int BUFFER_SIZE = 1024;

        static void Main(string[] args)
        {
            var gameService = new GameService(MAX_CONNECTION);
            var service = new CNetworkService(false);
            service.session_created_callback += gameService.OnSessionConnected;
            service.initialize(MAX_CONNECTION, BUFFER_SIZE);
            service.listen("127.0.0.1", 7979, 100);

            Console.WriteLine("Start Game Server");
            while (true)
            {
                //Console.Write(".");
                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;

                switch (input)
                {
                    case "users":
                    {
                        var totalUserCount = service.usermanager.get_total_count();
                        Console.WriteLine($"Total User Count: {totalUserCount}");
                        break;
                    }
                }

                System.Threading.Thread.Sleep(100);
            }
        }
    }
}
