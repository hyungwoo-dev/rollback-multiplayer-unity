using FreeNet;
using System;
using System.Collections.Generic;
using System.Net;

/// <summary>
/// FreeNet엔진과 유니티를 이어주는 클래스
/// FreeNet엔진에서 받은 접속 이벤트, 메시지 수신 이벤트등을 어플리케이션으로 전달하는 역할
/// </summary>
public class NetworkHandler
{
    static NetworkHandler()
    {
        CPacketBufferManager.initialize(4096);
    }

    private readonly object _packetQueueSyncLock = new();
    private Queue<CPacket> _packetQueue = new();
    private Queue<CPacket> _packetProcessQueue = new();

    private ServerPeer ServerPeer { get; set; }
    private CNetworkService Service { get; set; }

    public event Action<NetworkStatus> OnStatusChanged;
    public event Action<CPacket> OnMessage;

    public void ProcessPacket()
    {
        lock (_packetQueueSyncLock)
        {
            (_packetProcessQueue, _packetQueue) = (_packetQueue, _packetProcessQueue);
        }

        while (_packetProcessQueue.Count > 0)
        {
            var msg = _packetProcessQueue.Dequeue();
            OnMessage?.Invoke(msg);
        }
    }

    public void Connect(string host, int port)
    {
        // CNetworkService객체는 메시지의 비동기 송,수신 처리를 수행한다.
        Service = new CNetworkService();

        // EndPoint정보를 갖고있는 Connector생성
        CConnector connector = new CConnector(Service);
        connector.connected_callback += (serverToken) =>
        {
            ServerPeer = CreateServerPeer(serverToken);
            OnStatusChanged?.Invoke(NetworkStatus.CONNECTED);
        };

        OnStatusChanged?.Invoke(NetworkStatus.CONNECTING);

        var address = IPAddress.Parse(host);
        var endpoint = new IPEndPoint(address, port);
        connector.connect(endpoint);
    }

    public void Disconnect()
    {
        if (ServerPeer != null)
        {
            ServerPeer.Token.disconnect();
            OnStatusChanged?.Invoke(NetworkStatus.DISCONNECTED);
        }
    }

    public void Send(CPacket msg)
    {
        try
        {
            ((IPeer)ServerPeer).send(msg);
        }
        catch (Exception e)
        {
            Debug.Shared.LogException(e);
        }
    }

    private ServerPeer CreateServerPeer(CUserToken server_token)
    {
        var serverPeer = new ServerPeer(server_token);
        serverPeer.OnMessage += (CPacket packet) =>
        {
            lock (_packetQueueSyncLock)
            {
                _packetQueue.Enqueue(packet);
            }
        };
        serverPeer.OnDisconnected += () =>
        {
            OnStatusChanged?.Invoke(NetworkStatus.DISCONNECTED);
        };
        return serverPeer;
    }
}