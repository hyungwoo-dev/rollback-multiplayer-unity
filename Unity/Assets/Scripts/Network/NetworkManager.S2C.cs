using FreeNet;
using System.Collections.Generic;
using UnityEngine.Pool;

public partial class NetworkManager
{
    private Pool<S2C_MSG_GAME_START> GameStartMessagePool = new(() => new S2C_MSG_GAME_START());
    public delegate void S2C_GAME_START_DELEGATE(S2C_MSG_GAME_START msgGameStart);
    public event S2C_GAME_START_DELEGATE OnGameStart = null;

    private Pool<S2C_MSG_FRAME_EVENT> FrameEventMessagePool = new(() => new S2C_MSG_FRAME_EVENT());
    public delegate void S2C_FRAME_EVENT_DELEGATE(int frame, List<S2C_MSG_FRAME_EVENT> msgFrameEvent);
    public event S2C_FRAME_EVENT_DELEGATE OnFrameEvent = null;

    private Pool<S2C_MSG_INVALIDATE_HASH> InvalidateHashMessagePool = new(() => new S2C_MSG_INVALIDATE_HASH());
    public delegate void S2C_FRAME_INVALIDATE_HASH_DELEGATE(S2C_MSG_INVALIDATE_HASH msgInvalidateHash);
    public event S2C_FRAME_INVALIDATE_HASH_DELEGATE OnFrameInvalidateHash = null;

    private void OnMessage(CPacket msg)
    {
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
                S2C_FRAME_EVENTS(msg);
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

    private void S2C_FRAME_EVENTS(CPacket packet)
    {
        var messages = ListPool<S2C_MSG_FRAME_EVENT>.Get();
        var frame = packet.pop_int32();
        var length = packet.pop_int32();
        for (var i = 0; i < length; ++i)
        {
            var msgFrameEvent = FrameEventMessagePool.Get();
            ReadFrameEvent(packet, msgFrameEvent);
            messages.Add(msgFrameEvent);
        }

        OnFrameEvent?.Invoke(frame, messages);

        foreach (var message in messages)
        {
            FrameEventMessagePool.Release(message);
        }

        ListPool<S2C_MSG_FRAME_EVENT>.Release(messages);
    }

    private void S2C_INVALIDATE_HASH(CPacket packet)
    {
        var msgFrameInvalidateHash = InvalidateHashMessagePool.Get();

        msgFrameInvalidateHash.Frame = packet.pop_int32();
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
            msgFrameEvent.UserIndex = packet.pop_byte();
            msgFrameEvent.BattleTimeMillis = packet.pop_int32();
        }
        else
        {
            msgFrameEvent.UserIndex = 0;
        }
    }
}