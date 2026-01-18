using FreeNet;
using UnityEngine.Pool;

public partial class NetworkManager
{
    private ObjectPool<S2C_MSG_GAME_START> GameStartMessagePool = new(() => new S2C_MSG_GAME_START());
    public delegate void S2C_GAME_START_DELEGATE(S2C_MSG_GAME_START msgGameStart);
    public event S2C_GAME_START_DELEGATE OnGameStart = null;

    private ObjectPool<S2C_MSG_FRAME_EVENT> FrameEventMessagePool = new(() => new S2C_MSG_FRAME_EVENT());
    public delegate void S2C_FRAME_EVENT_DELEGATE(S2C_MSG_FRAME_EVENT msgFrameEvent);
    public event S2C_FRAME_EVENT_DELEGATE OnFrameEvent = null;

    private ObjectPool<S2C_MSG_INVALIDATE_HASH> InvalidateHashMessagePool = new(() => new S2C_MSG_INVALIDATE_HASH());
    public delegate void S2C_FRAME_INVALIDATE_HASH_DELEGATE(S2C_MSG_INVALIDATE_HASH msgInvalidateHash);
    public event S2C_FRAME_INVALIDATE_HASH_DELEGATE OnFrameInvalidateHash = null;

    private void OnMessage(CPacket msg)
    {
        Debug.Log($"OnMessage ProtocolID: {msg.protocol_id}");
        var protocol = (S2C_MSG)msg.pop_protocol_id();
        switch (protocol)
        {
            case S2C_MSG.S2C_START_GAME:
            {
                S2C_GAME_START(msg);
                break;
            }
            case S2C_MSG.S2C_FRAME_EVENT:
            {
                S2C_FRAME_EVENT(msg);
                break;
            }
            case S2C_MSG.S2C_INVALIDATE_HASH:
            {
                S2C_INVALIDATE_HASH(msg);
                break;
            }
        }
    }


    private void S2C_GAME_START(CPacket packet)
    {
        var msgStartGame = GameStartMessagePool.Get();

        msgStartGame.GameStartUnixTimeMillis = packet.pop_int64();
        msgStartGame.PlayerIndex = packet.pop_byte();
        msgStartGame.OpponentPlayerIndex = packet.pop_byte();
        OnGameStart?.Invoke(msgStartGame);

        GameStartMessagePool.Release(msgStartGame);
    }

    private void S2C_FRAME_EVENT(CPacket packet)
    {
        var length = packet.pop_byte();
        for (var i = 0; i < length; ++i)
        {
            var msgFrameEvent = FrameEventMessagePool.Get();

            ReadFrameEvent(packet, msgFrameEvent);
            OnFrameEvent?.Invoke(msgFrameEvent);

            FrameEventMessagePool.Release(msgFrameEvent);
        }
    }

    private void S2C_INVALIDATE_HASH(CPacket packet)
    {
        var msgFrameInvalidateHash = InvalidateHashMessagePool.Get();

        msgFrameInvalidateHash.PlayerHash = packet.pop_int32();
        msgFrameInvalidateHash.OpponentPlayerHash = packet.pop_int32();
        OnFrameInvalidateHash?.Invoke(msgFrameInvalidateHash);

        InvalidateHashMessagePool.Release(msgFrameInvalidateHash);
    }

    private static void ReadFrameEvent(CPacket packet, S2C_MSG_FRAME_EVENT msgFrameEvent)
    {
        msgFrameEvent.EventType = (FrameEventType)packet.pop_byte();
        if (msgFrameEvent.EventType != FrameEventType.NONE)
        {
            msgFrameEvent.Frame = packet.pop_int32();
            msgFrameEvent.EventOrder = packet.pop_byte();
            msgFrameEvent.UserIndex = packet.pop_byte();
        }
        else
        {
            msgFrameEvent.Frame = 0;
            msgFrameEvent.EventOrder = 0;
            msgFrameEvent.UserIndex = 0;
        }
    }
}