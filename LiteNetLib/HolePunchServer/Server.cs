using LiteNetLib;
using System.Net;

namespace HolePunchServer
{
    internal class WaitPeer
    {
        public IPEndPoint InternalAddr { get; }
        public IPEndPoint ExternalAddr { get; }
        public DateTime RefreshTime { get; private set; }

        public void Refresh()
        {
            RefreshTime = DateTime.UtcNow;
        }

        public WaitPeer(IPEndPoint internalAddr, IPEndPoint externalAddr)
        {
            Refresh();
            InternalAddr = internalAddr;
            ExternalAddr = externalAddr;
        }
    }

    class ServerNatPunchListener : INatPunchListener
    {
        public delegate void OnNatIntroductionRequestDelegate(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token);
        public required OnNatIntroductionRequestDelegate OnNatIntroductionRequest;

        public delegate void OnNatIntroductionSuccessDelegate(IPEndPoint targetEndPoint, NatAddressType type, string token);
        public required OnNatIntroductionSuccessDelegate OnNatIntroductionSuccess;

        void INatPunchListener.OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
        {
            OnNatIntroductionRequest?.Invoke(localEndPoint, remoteEndPoint, token);
        }

        void INatPunchListener.OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
        {
            OnNatIntroductionSuccess?.Invoke(targetEndPoint, type, token);
        }
    }

    internal class Server
    {
        private static readonly TimeSpan KickTime = new TimeSpan(0, 0, 30);

        private readonly object _peerLock = new();
        private readonly string _connectionKey;
        private readonly int _serverPort;
        private readonly Dictionary<string, WaitPeer> _waitingPeers = new();
        private readonly List<string> _peersToRemove = new();
        private readonly NetManager _puncher;

        public Server(string connectionKey, int serverPort)
        {
            _connectionKey = connectionKey;
            _serverPort = serverPort;

            var netListener = new EventBasedNetListener();
            netListener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("[Server] PeerConnected: " + peer);
            };

            netListener.ConnectionRequestEvent += request =>
            {
                request.AcceptIfKey(_connectionKey);
            };

            netListener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                Console.WriteLine("[Server] PeerDisconnected: " + disconnectInfo.Reason);
                if (disconnectInfo.AdditionalData.AvailableBytes > 0)
                {
                    Console.WriteLine("[Server] Disconnect data: " + disconnectInfo.AdditionalData.GetInt());
                }


            };

            _puncher = new NetManager(netListener)
            {
                IPv6Enabled = true,
                NatPunchEnabled = true
            };
        }

        public void Run()
        {
            _puncher.Start(_serverPort);
            _puncher.NatPunchModule.Init(new ServerNatPunchListener()
            {
                OnNatIntroductionRequest = HandleOnNatIntroductionRequest,
                OnNatIntroductionSuccess = HandleOnNatIntroductionSuccess,
            });

            // keep going until ESCAPE is pressed
            Console.WriteLine("Run Puncher");

            while (true)
            {
                try
                {
                    var nowTime = DateTime.UtcNow;

                    _puncher.NatPunchModule.PollEvents();
                    _puncher.PollEvents();

                    lock (_peerLock)
                    {
                        //check old peers
                        foreach (var waitPeer in _waitingPeers)
                        {
                            if (nowTime - waitPeer.Value.RefreshTime > KickTime)
                            {
                                _peersToRemove.Add(waitPeer.Key);
                            }
                        }

                        //remove
                        for (int i = 0; i < _peersToRemove.Count; i++)
                        {
                            Console.WriteLine("Kicking peer: " + _peersToRemove[i]);
                            _waitingPeers.Remove(_peersToRemove[i]);
                        }
                        _peersToRemove.Clear();
                    }
                    Thread.Sleep(1);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            _puncher.Stop();
        }

        private void HandleOnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
        {
            Console.WriteLine($"[Server] HandleOnNatIntroductionRequest - localEndPoint: {localEndPoint}, remoteEndPoint: {remoteEndPoint}, token: {token}");

            lock (_peerLock)
            {
                if (_waitingPeers.TryGetValue(token, out var wpeer))
                {
                    if (wpeer.InternalAddr.Equals(localEndPoint) &&
                        wpeer.ExternalAddr.Equals(remoteEndPoint))
                    {
                        wpeer.Refresh();
                        Console.WriteLine("[Server] Wait peer refresh");
                        return;
                    }

                    Console.WriteLine("[Server] Wait peer found, sending introduction...");

                    //found in list - introduce client and host to eachother
                    Console.WriteLine("[Server] host - i({0}) e({1})", wpeer.InternalAddr, wpeer.ExternalAddr);
                    Console.WriteLine("[Server] client - i({0}) e({1})", localEndPoint, remoteEndPoint);

                    _puncher.NatPunchModule.NatIntroduce(
                        wpeer.InternalAddr, // host internal
                        wpeer.ExternalAddr, // host external
                        localEndPoint, // client internal
                        remoteEndPoint, // client external
                        token // request token
                        );

                    //Clear dictionary
                    _waitingPeers.Remove(token);
                }
                else
                {
                    Console.WriteLine("[Server] Wait peer created. i({0}) e({1})", localEndPoint, remoteEndPoint);
                    _waitingPeers[token] = new WaitPeer(localEndPoint, remoteEndPoint);
                }
            }
        }

        private void HandleOnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
        {
            Console.WriteLine($"[Server] HandleOnNatIntroductionSuccess - targetEndPoint: {targetEndPoint}, type: {type}, token: {token}");
        }
    }
}
