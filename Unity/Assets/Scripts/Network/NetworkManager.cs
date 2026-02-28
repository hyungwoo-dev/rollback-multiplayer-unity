public partial class NetworkManager
{
    private static Debug Debug { get; } = new Debug(nameof(NetworkManager));

    private LiteNetClient _client = new LiteNetClient();

    public LiteNetState State => _client.CurrentState;

    public NetworkManager()
    {
        _client.OnEnterWorld += HandleOnEnterWorld;
        _client.OnIntermidiateFrameEvent += HandleOnIntermidiateFrameEvent;
        _client.OnFrameEvents += HandleOnFrameEvents;
        _client.OnFrameHash += HandleOnFrameHash;
    }

    public void Connect(string address, int port)
    {
        if (_client.CurrentState != LiteNetState.NONE)
        {
            return;
        }

        _client.Connect(address, port, "TOKEN");
    }

    public void PollEvents()
    {
        if (_client.CurrentState == LiteNetState.P2P_CONNECTED)
        {
            _client.PollEvents();
        }
    }

    public void Disconnect()
    {
        if (_client.CurrentState == LiteNetState.P2P_DISCONNECTED)
        {
            return;
        }

        _client.Disconnect();
    }

    public void Dispose()
    {
        if (_client.CurrentState == LiteNetState.P2P_DISCONNECTED)
        {
            return;
        }

        _client.OnEnterWorld -= HandleOnEnterWorld;
        _client.OnIntermidiateFrameEvent -= HandleOnIntermidiateFrameEvent;
        _client.OnFrameEvents -= HandleOnFrameEvents;
        _client.OnFrameHash -= HandleOnFrameHash;
        _client.Disconnect();
    }
}
