using System;

namespace HolePunchServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const string CONNECTION_KEY = "test_key";
            const int SERVER_PORT = 50010;
            Thread serverThread = new Thread(state =>
            {
                Server? server = (Server)state;
                server?.Run();
            });
            serverThread.IsBackground = true;
            serverThread.Priority = ThreadPriority.Highest;
            serverThread.Start(new Server(CONNECTION_KEY, SERVER_PORT));

            Thread.Sleep(1000);

            var clients = new List<Client>();
            var clientID = 0;

            var isRunning = true;
            while (isRunning)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.Escape:
                        {
                            Console.WriteLine("[System] Escape");
                            isRunning = false;
                            break;
                        }
                        case ConsoleKey.A:
                        {
                            Console.WriteLine("[System] Add client");

                            var client = new Client(clientID.ToString(), CONNECTION_KEY, SERVER_PORT);
                            var evenNumber = clientID % 2 == 0 ? clientID : clientID - 1;
                            var token = evenNumber.ToString();
                            var thread = new Thread(state =>
                            {
                                Client? newClient = (Client)state;
                                newClient?.Start(token);
                            });
                            thread.IsBackground = true;
                            thread.Start(client);

                            clientID += 1;
                            break;
                        }
                        case ConsoleKey.C:
                        {
                            Console.WriteLine("Stop all clients");
                            clientID = 0;
                            foreach (var client in clients)
                            {
                                client.Stop();
                            }
                            clients.Clear();
                            break;
                        }
                    }
                }

                Thread.Sleep(10);
            }
        }
    }
}
