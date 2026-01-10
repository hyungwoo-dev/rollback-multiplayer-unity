using FreeNet;
using System.Diagnostics;
using UnityEngine;

public partial class NetworkManager : MonoBehaviour
{
    private readonly Debug _debug = new Debug(nameof(NetworkHandler));

    [SerializeField]
    private string _address = "127.0.0.1";

    [SerializeField]
    private int _port = 7979;

    private NetworkHandler Handler { get; set; }
    private NetworkStatus Status { get; set; } = NetworkStatus.NONE;

    private void Awake()
    {
        Handler = new NetworkHandler(4096);
        Handler.OnStatusChanged += OnStatusChanged;
        Handler.OnMessage += OnMessage;
    }

    private void Start()
    {
        Handler.Connect(_address, _port);
    }

    private void Update()
    {
        Handler.ProcessPacket();
    }

    private void OnDestroy()
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
        _debug.Log($"OnStatusChanged Status: {status}");
        Status = status;
        switch (status)
        {
            case NetworkStatus.CONNECTED:
            {
                RPC_REQUEST(0);
                break;
            }
            case NetworkStatus.DISCONNECTED:
            {
                break;
            }
        }
    }

    private void OnMessage(CPacket msg)
    {
        _debug.Log($"OnMessage ProtocolID: {msg.protocol_id}");
        var protocol = (Protocol)msg.pop_protocol_id();
        switch (protocol)
        {
            case Protocol.RESPONSE:
            {
                int number = msg.pop_int32();
                RPC_REQUEST(number + 1);
            }
            break;
        }
    }

    public void Send(CPacket msg)
    {
        if (Status != NetworkStatus.CONNECTED) return;

        _debug.Log($"SendPacket Protocol ID: {msg.protocol_id}");
        this.Handler.Send(msg);
    }
}