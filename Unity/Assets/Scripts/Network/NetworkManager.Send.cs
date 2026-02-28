public partial class NetworkManager
{
    public void SendEnterWorld(ref P2P_ENTER_WORLD message)
    {
        _client.SendToPeer(LiteNetProtocol.ENTER_WORLD, ref message);
    }

    public void SendIntermidiateFrameEvent(ref P2P_INTERMIDIATE_FRAME_EVENT message)
    {
        _client.SendToPeer(LiteNetProtocol.INTERMIDIATE_FRAME_EVENT, ref message);
    }

    public void SendFrameEvents(ref P2P_FRAME_EVENTS message)
    {
        _client.SendToPeer(LiteNetProtocol.FRAME_EVENTS, ref message);
    }

    public void SendFrameHash(ref P2P_FRAME_HASH message)
    {
        _client.SendToPeer(LiteNetProtocol.FRAME_HASH, ref message);
    }
}