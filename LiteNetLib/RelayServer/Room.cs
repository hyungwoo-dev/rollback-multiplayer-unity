using LiteNetLib;
using LiteNetLib.Utils;

namespace RelayServer;

public class Room
{
    private static int _roomIdCounter = 1;

    private (NetPeer Peer, bool Alive) _peer1;
    private (NetPeer Peer, bool Alive) _peer2;

    public int RoomId { get; }

    public int Peer1Id => _peer1.Peer.Id;
    public int Peer2Id => _peer2.Peer.Id;
    public bool IsAvailable => _peer1.Alive && _peer2.Alive;
    public bool IsAllPeerDisconnected => !_peer1.Alive && !_peer2.Alive;

    public Room(NetPeer peer1, NetPeer peer2)
    {
        _peer1 = (peer1, true);
        _peer2 = (peer2, true);
        RoomId = _roomIdCounter++;
    }

    public void Start()
    {
        var netDataWriter = new NetDataWriter();
        netDataWriter.Put("Start");
        _peer1.Peer.Send(netDataWriter,  DeliveryMethod.ReliableOrdered);
        _peer2.Peer.Send(netDataWriter,  DeliveryMethod.ReliableOrdered);
    }

    public bool ReleasePeer(int peerId)
    {
        if (_peer1.Peer.Id == peerId)
        {
            _peer1.Alive = false;
            return true;
        }

        if (_peer2.Peer.Id == peerId)
        {
            _peer2.Alive = false;
            return true;
        }

        return false;
    }

    public bool SendMessage(int senderId, ReadOnlySpan<byte> data)
    {
        if (_peer1.Peer.Id == senderId)
        {
            _peer2.Peer.Send(data, DeliveryMethod.ReliableOrdered);
            return true;
        }
        if (_peer2.Peer.Id == senderId)
        {
            _peer1.Peer.Send(data, DeliveryMethod.ReliableOrdered);
            return true;
        }

        return false;
    }
}
