using LiteNetLib.Utils;
using System.Collections.Generic;

public enum LiteNetProtocol : byte
{
    ENTER_WORLD = 0,
    INTERMIDIATE_FRAME_EVENT,
    FRAME_EVENTS,
    FRAME_HASH,
}


public struct P2P_ENTER_WORLD : INetSerializable
{
    public static P2P_ENTER_WORLD Create()
    {
        return new P2P_ENTER_WORLD();
    }

    public long EnterUnixTimeMillis;
    public int RandomValue;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(EnterUnixTimeMillis);
        writer.Put(RandomValue);
    }

    public void Deserialize(NetDataReader reader)
    {
        EnterUnixTimeMillis = reader.GetLong();
        RandomValue = reader.GetInt();
    }
}

public struct P2P_INTERMIDIATE_FRAME_EVENT : INetSerializable
{
    public static P2P_INTERMIDIATE_FRAME_EVENT Create()
    {
        return new P2P_INTERMIDIATE_FRAME_EVENT();
    }

    /// <summary>
    /// 이벤트가 작동할 프레임
    /// </summary>
    public int Frame;

    /// <summary>
    /// 이 프레임에 적용할 이벤트
    /// </summary>
    public P2P_FRAME_EVENT FrameEvent;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Frame);
        FrameEvent.Serialize(writer);
    }

    public void Deserialize(NetDataReader reader)
    {
        Frame = reader.GetInt();
        FrameEvent = new P2P_FRAME_EVENT();
        FrameEvent.Deserialize(reader);
    }
}

/// <summary>
/// 확정된 상태의 이벤트 정보
/// </summary>
public struct P2P_FRAME_EVENTS : INetSerializable
{
    public static P2P_FRAME_EVENTS Create()
    {
        return new P2P_FRAME_EVENTS()
        {
            Events = new List<P2P_FRAME_EVENT>()
        };
    }

    /// <summary>
    /// 이벤트가 작동할 프레임
    /// </summary>
    public int Frame;

    /// <summary>
    /// 이 프레임에 적용할 이벤트 목록
    /// </summary>
    public List<P2P_FRAME_EVENT> Events;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Frame);
        writer.Put(Events.Count);
        for (int i = 0; i < Events.Count; i++)
        {
            Events[i].Serialize(writer);
        }
    }

    public void Deserialize(NetDataReader reader)
    {
        Frame = reader.GetInt();
        var count = reader.GetInt();
        for (var i = 0; i < count; ++i)
        {
            var newEvent = new P2P_FRAME_EVENT();
            newEvent.Deserialize(reader);
            Events.Add(newEvent);
        }
    }
}

public struct P2P_FRAME_EVENT : INetSerializable
{
    public static P2P_FRAME_EVENT Create()
    {
        return new P2P_FRAME_EVENT();
    }

    /// <summary>
    /// 이벤트 유형, 이 유형이 None이라면 나머지 데이터는 파싱하지 않는다.
    /// </summary>
    public FrameEventType EventType;

    /// <summary>
    /// 입력을 처리했던 시간 (Milliseconds)
    /// </summary>
    public int BattleTimeMillis;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put((byte)EventType);
        writer.Put(BattleTimeMillis);
    }

    public void Deserialize(NetDataReader reader)
    {
        EventType = (FrameEventType)reader.GetByte();
        BattleTimeMillis = reader.GetInt();
    }
}

public struct P2P_FRAME_HASH : INetSerializable
{
    public static P2P_FRAME_HASH Create()
    {
        return new P2P_FRAME_HASH();
    }

    /// <summary>
    /// 검증할 프레임
    /// </summary>
    public int Frame;

    /// <summary>
    /// 검증할 프레임의 상태 해시
    /// </summary>
    public long Hash;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Frame);
        writer.Put(Hash);
    }

    public void Deserialize(NetDataReader reader)
    {
        Frame = reader.GetInt();
        Hash = reader.GetLong();
    }
}