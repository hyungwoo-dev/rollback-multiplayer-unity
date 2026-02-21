using FreeNet;
using System.Threading;
using System.Threading.Tasks;

public partial class NetworkManager
{
    private static Debug Debug { get; } = new Debug(nameof(NetworkManager));

    private long _statusIntValue;

    private NetworkHandler Handler { get; set; }
    public NetworkStatus Status => (NetworkStatus)Interlocked.Read(ref _statusIntValue);

    private CancellationTokenSource _processPacketCancellationToken;

    public NetworkManager()
    {
        Handler = new NetworkHandler();
        Handler.OnStatusChanged += OnStatusChanged;
        Handler.OnMessage += OnMessage;
    }

    public void Connect(string address, int port)
    {
        if(Status == NetworkStatus.CONNECTING || Status == NetworkStatus.CONNECTED)
        {
            return;
        }

        Handler.Connect(address, port);

        var processPacketCancellationToken = new CancellationTokenSource();
        Task.Run(() => ProcessPacket(this, processPacketCancellationToken));
        _processPacketCancellationToken = processPacketCancellationToken;
    }

    public void Disconnect()
    {
        if (Status == NetworkStatus.DISCONNECTED)
        {
            return;
        }

        if (_processPacketCancellationToken != null)
        {
            _processPacketCancellationToken.Cancel();
            _processPacketCancellationToken = null;
        }
        Handler.Disconnect();
    }

    public void Dispose()
    {
        Handler.OnStatusChanged -= OnStatusChanged;
        Handler.OnMessage -= OnMessage;
        Handler.Disconnect();
    }

    /// <summary>
    /// 네트워크 상태 변경시 호출될 콜백 매소드 (워커 스레드에서 호출될 수 있음)
    /// </summary>
    /// <param name="server_token"></param>
    private void OnStatusChanged(NetworkStatus status)
    {
        Debug.Log($"OnStatusChanged Status: {status}");
        Interlocked.Exchange(ref _statusIntValue, (long)status);
    }

    private void Send(CPacket msg)
    {
        if (Status != NetworkStatus.CONNECTED) return;

        Debug.Log($"SendPacket Protocol ID: {msg.protocol_id}");
        this.Handler.Send(msg);
    }

    private static void ProcessPacket(NetworkManager networkManager, CancellationTokenSource cancellationTokenSource)
    {
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            if (networkManager.Status == NetworkStatus.CONNECTED)
            {
                networkManager.Handler.ProcessPacket();
            }

            Thread.Sleep(1);
        }
    }
}
