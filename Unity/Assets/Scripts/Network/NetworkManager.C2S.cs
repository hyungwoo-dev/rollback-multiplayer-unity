using FreeNet;
using System.Collections.Generic;
using UnityEngine.Pool;

public partial class NetworkManager
{
    public ObjectPool<C2S_MSG_FRAME_EVENTS> C2S_FrameEventsPool = new ObjectPool<C2S_MSG_FRAME_EVENTS>(() => new C2S_MSG_FRAME_EVENTS());
    public ObjectPool<C2S_MSG_FRAME_EVENT> C2S_FrameEventPool = new ObjectPool<C2S_MSG_FRAME_EVENT>(() => new C2S_MSG_FRAME_EVENT());

    public void C2S_ENTER_WORLD(C2S_MSG_ENTER_WORLD msgEnterWorld)
    {
        CPacket packet = new CPacket();
        packet.set_protocol((short)C2S_MSG.ENTER_WORLD);
        packet.push(msgEnterWorld.EnterUnixTimeMillis);

        Send(packet);
    }

    public void C2S_FRAME_EVENTS(C2S_MSG_FRAME_EVENTS msgFrameEvents)
    {
        CPacket packet = new CPacket();
        packet.set_protocol((short)C2S_MSG.FRAME_EVENT);
        packet.push(msgFrameEvents.Frame);
        packet.push(msgFrameEvents.Events.Count);
        for (int i = 0; i < msgFrameEvents.Events.Count; i++)
        {
            packet.push((byte)msgFrameEvents.Events[i].EventType);
            packet.push(msgFrameEvents.Events[i].BattleTimeMillis);
        }

        Send(packet);
    }

    public void C2S_FRAME_HASH(C2S_MSG_FRAME_HASH msgFrameHash)
    {
        CPacket packet = new CPacket();
        packet.set_protocol((short)C2S_MSG.FRAME_HASH);
        packet.push(msgFrameHash.Frame);
        packet.push(msgFrameHash.Hash);

        Send(packet);
    }
}