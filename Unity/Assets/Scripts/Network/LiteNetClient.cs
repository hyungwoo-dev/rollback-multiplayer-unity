using LiteNetLib;
using LiteNetLib.Utils;
using System.Threading;

public enum LiteNetState
{
    NONE = 0,
    P2P_HOLE_PUNCHING,
    P2P_CONNECTED,
    P2P_DISCONNECTED,
}

public class P2pPeer
{
    public NetPeer Peer;
    public NatAddressType NatAddressType;
    public long ConnectedUnixTimeMillis;
}

public class LiteNetClient
{
    private static Debug Debug = new Debug(nameof(LiteNetClient));
    private const string ConnectionKey = "test_key";

    private object _sendLock = new();
    private NetManager _netManager;
    private long _currentState;
    public LiteNetState CurrentState => (LiteNetState)Interlocked.Read(ref _currentState);
    private NetDataWriter _writer = new NetDataWriter(true, 1024);

    public delegate void EnterWorldDelegate(in P2P_ENTER_WORLD message);
    public EnterWorldDelegate OnEnterWorld;

    public delegate void IntermidiateFrameEventDelegate(in P2P_INTERMIDIATE_FRAME_EVENT message);
    public IntermidiateFrameEventDelegate OnIntermidiateFrameEvent;

    public delegate void FrameEventsDelegate(in P2P_FRAME_EVENTS message);
    public FrameEventsDelegate OnFrameEvents;

    public delegate void FrameHashDelegate(in P2P_FRAME_HASH message);
    public FrameHashDelegate OnFrameHash;

    internal LiteNetClient()
    {
        var clientListener = new EventBasedNetListener();
        clientListener.PeerConnectedEvent += HandlePeerConnectedEvent;
        clientListener.ConnectionRequestEvent += HandleOnConnectionRequestEvent;
        clientListener.PeerDisconnectedEvent += HandleOnPeerDisconnectedEvent;
        clientListener.NetworkReceiveEvent += HandleOnNetworkReceiveEvent;

        _netManager = new NetManager(clientListener)
        {
            IPv6Enabled = true,
            NatPunchEnabled = true
        };
    }

    #region Net Listener

    private void HandlePeerConnectedEvent(NetPeer peer)
    {
        Debug.Log($"[LiteNetClient] PeerConnected: " + peer);
    }

    private static void HandleOnConnectionRequestEvent(ConnectionRequest request)
    {
        request.AcceptIfKey(ConnectionKey);
    }

    private void HandleOnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log($"[LiteNetClient] PeerDisconnected: {disconnectInfo.Reason} Address: {peer.Address}:{peer.Port}");
        if (disconnectInfo.AdditionalData.AvailableBytes > 0)
        {
            Debug.Log($"[LiteNetClient] Disconnect data: {disconnectInfo.AdditionalData.GetInt()}");
        }
    }

    private void HandleOnNetworkReceiveEvent(NetPeer fromPeer, NetPacketReader dataReader, byte channel, DeliveryMethod deliveryMethod)
    {
        var protocol = (LiteNetProtocol)dataReader.GetByte();

        switch (protocol)
        {
            case LiteNetProtocol.ENTER_WORLD:
            {
                var message = P2P_ENTER_WORLD.Create();
                message.Deserialize(dataReader);
                OnEnterWorld?.Invoke(message);
                break;
            }
            case LiteNetProtocol.INTERMIDIATE_FRAME_EVENT:
            {
                var message = P2P_INTERMIDIATE_FRAME_EVENT.Create();
                message.Deserialize(dataReader);
                OnIntermidiateFrameEvent?.Invoke(message);
                break;
            }
            case LiteNetProtocol.FRAME_EVENTS:
            {
                var message = P2P_FRAME_EVENTS.Create();
                message.Deserialize(dataReader);
                OnFrameEvents?.Invoke(message);
                break;
            }
            case LiteNetProtocol.FRAME_HASH:
            {
                var message = P2P_FRAME_HASH.Create();
                message.Deserialize(dataReader);
                OnFrameHash?.Invoke(message);
                break;
            }
        }
        dataReader.Recycle();
    }

    #endregion Net Listener

    public void Connect(string address, int serverPort, string token)
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            SetState(LiteNetState.P2P_HOLE_PUNCHING);

            Debug.Log($"[LiteNetClient] Start");

            var natPunchListener = new EventBasedNatPunchListener();
            natPunchListener.NatIntroductionSuccess += (point, addrType, token) =>
            {
                var peer = _netManager.Connect(point, ConnectionKey);
                Debug.Log($"[LiteNetClient] NatIntroductionSuccess. Connecting to point: {point}, type: {addrType}, connection created: {peer != null}");
            };
            natPunchListener.NatIntroductionRequest += (targetEndPoint, type, token) =>
            {
                Debug.Log($"[LiteNetClient] NatIntroductionRequest - TargetEndPoint: {targetEndPoint}, type: {type}, token: {token}");
            };
            _netManager.NatPunchModule.Init(natPunchListener);
            _netManager.Start();

            _netManager.NatPunchModule.SendNatIntroduceRequest(address, serverPort, token);

            while (CurrentState != LiteNetState.P2P_CONNECTED)
            {
                PollEvents();

                switch (CurrentState)
                {
                    case LiteNetState.P2P_HOLE_PUNCHING:
                    {
                        if (_netManager.ConnectedPeersCount >= 1)
                        {
                            SetState(LiteNetState.P2P_CONNECTED);
                        }
                        break;
                    }
                }
            }
        });
    }

    public void PollEvents()
    {
        _netManager.NatPunchModule.PollEvents();
        _netManager.PollEvents();
    }

    public void Disconnect()
    {
        Debug.Log("Pre Stop");
        _netManager.Stop();

        Debug.Log("Pre DisconnectAll");
        _netManager.DisconnectAll();

        Debug.Log("Disconnect Finished");
    }

    public void SendToPeer<T>(LiteNetProtocol protocol, ref T message) where T : INetSerializable
    {
        Send(_netManager.FirstPeer, protocol, ref message);
    }

    private void SetState(LiteNetState state)
    {
        Debug.Log($"[LiteNetClient] SetState: {state}");
        Interlocked.Exchange(ref _currentState, (long)state);
    }

    private void Send<T>(NetPeer peer, LiteNetProtocol protocol, ref T message) where T : INetSerializable
    {
        lock (_sendLock)
        {
            _writer.Put((byte)protocol);
            message.Serialize(_writer);

            peer.Send(_writer, DeliveryMethod.ReliableOrdered);

            _writer.SetPosition(0);
        }
    }
}