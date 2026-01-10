using System;
using FreeNet;

public class ServerPeer : IPeer
{
    public CUserToken Token { get; }

    public Action<CPacket> OnMessage = null;
    public Action OnDisconnected = null;

    public ServerPeer(CUserToken token)
    {
        Token = token;
        Token.set_peer(this);
    }

    void IPeer.on_message(CPacket msg)
    {
        OnMessage?.Invoke(msg);
    }

    void IPeer.on_removed()
    {
        OnDisconnected?.Invoke();
    }

    void IPeer.send(CPacket msg)
    {
        Token.send(msg);
    }

    void IPeer.disconnect()
    {

    }
}