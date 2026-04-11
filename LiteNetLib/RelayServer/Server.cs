using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;

namespace RelayServer;

public class Server
{
    private int _port;
    private object _peerLock = new();
    private List<NetPeer> _peers = new();
    private List<NetPeer> _tempPeers = new();

    private Queue<NetPeer> _matchingPeers = new();

    private ConcurrentDictionary<int, Room> _rooms = new();
    private ConcurrentDictionary<int, Room> _roomsByPeerId = new();

    public Server(int port)
    {
        _port = port;
    }

    public void Start(CancellationToken token)
    {
        var listener = new EventBasedNetListener();
        var server = new NetManager(listener);
        server.Start(_port);

        listener.PeerConnectedEvent += OnListenerOnPeerConnectedEvent;
        listener.PeerDisconnectedEvent += OnListenerOnPeerDisconnectedEvent;
        listener.NetworkReceiveEvent += OnListenerOnNetworkReceiveEvent;
        listener.NetworkErrorEvent += OnListenerOnNetworkErrorEvent;

        while (!token.IsCancellationRequested && !Console.KeyAvailable)
        {
            server.PollEvents();
            Thread.Sleep(1);
        }
        server.Stop();
    }

    public void StartMatching(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            lock (_peerLock)
            {
                (_peers, _tempPeers) = (_tempPeers, _peers);
            }

            foreach (var netPeer in _tempPeers)
            {
                _matchingPeers.Enqueue(netPeer);
            }
            _tempPeers.Clear();

            while (_matchingPeers.Count > 2)
            {
                var peer1 = _matchingPeers.Dequeue();
                var peer2 = _matchingPeers.Dequeue();
                var room = new Room(peer1, peer2);
                _rooms.TryAdd(room.RoomId, room);

                _roomsByPeerId.TryAdd(peer1.Id, room);
                _roomsByPeerId.TryAdd(peer2.Id, room);

                room.Start();
            }

            Thread.Sleep(1);
        }
    }

    private void OnListenerOnPeerConnectedEvent(NetPeer peer)
    {
        Console.WriteLine($"OnPeerConnectedEvent, Id: {peer.Id}, IP: {peer.Address}:{peer.Port}");
        lock (_peerLock)
        {
            _peers.Add(peer);
        }
    }

    private void OnListenerOnPeerDisconnectedEvent(NetPeer peer, DisconnectInfo info)
    {
        Console.WriteLine($"OnPeerDisconnectedEvent, PeerId: {peer.Id}, Info.Reason: {info.Reason}, Info.SocketError: {info.SocketErrorCode}");

        if (_roomsByPeerId.TryRemove(peer.Id, out var room))
        {
            room.ReleasePeer(peer.Id);
            if (room.IsAllPeerDisconnected)
            {
                _rooms.Remove(room.RoomId, out _);
            }
        }
    }

    private void OnListenerOnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method)
    {
        Console.WriteLine($"OnNetworkReceiveEvent, PeerId: {peer.Id}, Channel: {channel}, Method: {method}");

        if (_roomsByPeerId.TryGetValue(peer.Id, out var room) && room.IsAvailable)
        {
            var message = ReadData(reader);
            room.SendMessage(peer.Id, message);
        }
    }

    private ArraySegment<byte> ReadData(NetDataReader reader)
    {
        var length = BitConverter.ToUInt16(reader.RawData, 0);
        reader.SetPosition(2);
        var bytesSegment = reader.GetBytesSegment(length);
        return bytesSegment;
    }

    private void OnListenerOnNetworkErrorEvent(IPEndPoint point, SocketError error)
    {
        Console.WriteLine($"OnNetworkErrorEvent, IPEndPoint: {point}, SocketError: {error}");
    }
}
