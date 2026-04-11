namespace RelayServer;

internal class Program
{
    static void Main(string[] args)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var serverThread = new Thread(state =>
        {
            var tokenSource = (CancellationTokenSource)state!;
            var server = new Server(50011);
            server.Start(tokenSource.Token);
            server.StartMatching(tokenSource.Token);
        });
        serverThread.IsBackground = true;
        serverThread.Priority = ThreadPriority.Highest;
        serverThread.Start(cancellationTokenSource);

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.Escape:
                    {
                        cancellationTokenSource.Cancel();
                        break;
                    }
                }
            }

            Thread.Sleep(100);
        }
    }
}
