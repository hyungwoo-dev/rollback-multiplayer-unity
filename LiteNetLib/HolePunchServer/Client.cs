using LiteNetLib;
using LiteNetLib.Utils;
using System.ComponentModel.Design;
using System.Net;
using System.Runtime.InteropServices;

namespace HolePunchServer
{
    internal enum ClientState
    {
        NONE = 0,
        STARTING,
        P2P_CONNECTED,
    }

    internal enum ClientProtocol : byte
    {
        CHECK_RTT_REQUEST = 0,
        CHECK_RTT_RESPONSE = 1,
    }

    internal class P2pPeer
    {
        public NetPeer Peer;
        public NatAddressType NatAddressType;
        public long ConnectedUnixTimeMillis;
    }

    internal class Client
    {
        private const string ConnectionKey = "test_key";

        private readonly string _name;
        private readonly string _connectionKey;
        private readonly int _serverPort;
        private NetManager _netManager;
        private long _runningFlag = 0;

        private ClientState _currentState = ClientState.NONE;

        private NetDataWriter _writer = new NetDataWriter(true, 1024);
        
        internal Client(string name, string connectionKey, int serverPort)
        {
            _name = name;
            _connectionKey = connectionKey;
            _serverPort = serverPort;

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

        private NetPeer _serverPeer;
        private List<P2pPeer> _peers = new();

        private void HandlePeerConnectedEvent(NetPeer peer)
        {
            if (peer.Port == _serverPort)
            {
                _serverPeer = peer;
            }
            Console.WriteLine($"[Client::{_name}] PeerConnected: " + peer);
        }

        private static void HandleOnConnectionRequestEvent(ConnectionRequest request)
        {
            request.AcceptIfKey(ConnectionKey);
        }

        private void HandleOnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            _peers.RemoveAll(p2pPeer => p2pPeer.Peer.Id == peer.Id);
            if (peer.Port == _serverPort)
            {
                _serverPeer = null;
            }

            Console.WriteLine($"[Client::{_name}] PeerDisconnected: {disconnectInfo.Reason} Address: {peer.Address}:{peer.Port}");
            if (disconnectInfo.AdditionalData.AvailableBytes > 0)
            {
                Console.WriteLine($"[Client::{_name}] Disconnect data: {disconnectInfo.AdditionalData.GetInt()}");
            }
        }

        private void HandleOnNetworkReceiveEvent(NetPeer fromPeer, NetPacketReader dataReader, byte channel, DeliveryMethod deliveryMethod)
        {
            var protocol = (ClientProtocol)dataReader.GetByte();
            Console.WriteLine($"[Client::{_name}] HandleOnNetworkReceiveEvent Protocol: {protocol}");
            switch (protocol)
            {
                case ClientProtocol.CHECK_RTT_REQUEST:
                {
                    var request = dataReader.Get<P2P_CHECK_RTT_REQUEST>();
                    var message = new P2P_CHECK_RTT_RESPONSE()
                    {
                        RequestUnixTimeMillis = request.RequestUnixTimeMillis,
                        ResponseUnixTimeMillis = GetNowUnixTimeMillis()
                    };
                    Send(fromPeer, ClientProtocol.CHECK_RTT_RESPONSE, ref message);
                    break;
                }
                case ClientProtocol.CHECK_RTT_RESPONSE:
                {
                    var request = dataReader.Get<P2P_CHECK_RTT_RESPONSE>();
                    var message = new P2P_CHECK_RTT_REQUEST()
                    {
                        RequestUnixTimeMillis = GetNowUnixTimeMillis(),
                    };
                    Send(fromPeer, ClientProtocol.CHECK_RTT_REQUEST, ref message);
                    break;
                }
            }
            dataReader.Recycle();
        }

        #endregion Net Listener

        private NetPeer _p2pPeer;

        public void Start(string token)
        {
            SetState(ClientState.STARTING);

            Interlocked.Increment(ref _runningFlag);

            Console.WriteLine($"[Client::{_name}] Start");

            var natPunchListener = new EventBasedNatPunchListener();
            natPunchListener.NatIntroductionSuccess += (point, addrType, token) =>
            {
                var peer = _netManager.Connect(point, ConnectionKey);
                Console.WriteLine($"[Client::{_name}] NatIntroductionSuccess. Connecting to point: {point}, type: {addrType}, connection created: {peer != null}");

                if (peer != null)
                {
                    _peers.Add(new P2pPeer()
                    {
                        Peer = peer,
                        NatAddressType = addrType,
                        ConnectedUnixTimeMillis = GetNowUnixTimeMillis(),
                    });
                }
            };
            natPunchListener.NatIntroductionRequest += (targetEndPoint, type, token) =>
            {
                Console.WriteLine($"[Client::{_name}] NatIntroductionRequest - TargetEndPoint: {targetEndPoint}, type: {type}, token: {token}");
            };
            _netManager.NatPunchModule.Init(natPunchListener);
            _netManager.Start();

            _netManager.NatPunchModule.SendNatIntroduceRequest("localhost", _serverPort, token);
        
            while (Interlocked.Read(ref _runningFlag) > 0)
            {
                Thread.Sleep(1);

                _netManager.NatPunchModule.PollEvents();
                _netManager.PollEvents();

                if (_netManager.ConnectedPeersCount >= 1 && _serverPeer == null)
                {

                }
                if (!_sent && _netManager.ConnectedPeersCount >= 1)
                {
                    
                    var message = new P2P_CHECK_RTT_REQUEST()
                    {
                        RequestUnixTimeMillis = GetNowUnixTimeMillis(),
                    };
                    
                    Send(_netManager.FirstPeer, ClientProtocol.CHECK_RTT_REQUEST, ref message);
                    _sent = true;
                }
            }

            _netManager.Stop();
        }

        private bool _sent = false;

        public void Stop()
        {
            Interlocked.Decrement(ref _runningFlag);
        }

        private P2pPeer GetFirstConnectedPeer()
        {
            P2pPeer firstConnectedPeer = null;
            foreach (var peer in _peers)
            {
                if (firstConnectedPeer == null)
                {
                    firstConnectedPeer = peer;
                }
                else if (peer.ConnectedUnixTimeMillis < firstConnectedPeer.ConnectedUnixTimeMillis)
                {
                    firstConnectedPeer = peer;
                }
            }
            return firstConnectedPeer;
        }

        private void SetState(ClientState state)
        {
            Console.WriteLine($"[Client::{_name}] SetState: {state}");
            _currentState = state;

            if (state == ClientState.P2P_CONNECTED)
            {
                var message = new P2P_CHECK_RTT_REQUEST()
                {
                    RequestUnixTimeMillis = GetNowUnixTimeMillis()
                };

                Send(_p2pPeer, ClientProtocol.CHECK_RTT_REQUEST, ref message);
            }
        }

        private long GetNowUnixTimeMillis()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private void Send<T>(NetPeer peer, ClientProtocol protocol, ref T message) where T : INetSerializable
        {
            Console.WriteLine($"[Client::{_name}] Send Protocol: {protocol}");

            _writer.Put((byte)protocol);
            message.Serialize(_writer);

            peer.Send(_writer, DeliveryMethod.ReliableOrdered);

            _writer.SetPosition(0);
        }

        private void SendToAll<T>(ClientProtocol protocol, ref T message) where T : INetSerializable
        {
            Console.WriteLine($"[Client::{_name}] Send To All ({_peers.Count}) Protocol: {protocol}");

            _writer.Put((byte)protocol);
            message.Serialize(_writer);
            foreach (var peer in _peers)
            {
                Send(peer.Peer, protocol, ref message);
            }

            _writer.SetPosition(0);

        }
    }
}
