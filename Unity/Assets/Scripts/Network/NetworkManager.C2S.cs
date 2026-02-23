using FreeNet;
using System.Collections.Generic;

public partial class NetworkManager
{
    public Pool<C2S_MSG_FRAME_EVENTS> C2S_FrameEventsPool = new Pool<C2S_MSG_FRAME_EVENTS>(() => new C2S_MSG_FRAME_EVENTS());
    public Pool<C2S_MSG_FRAME_EVENT> C2S_FrameEventPool = new Pool<C2S_MSG_FRAME_EVENT>(() => new C2S_MSG_FRAME_EVENT());
    public Pool<List<C2S_MSG_FRAME_EVENT>> C2S_FrameEventListPool = new Pool<List<C2S_MSG_FRAME_EVENT>>(() => new List<C2S_MSG_FRAME_EVENT>(), (list) => list.Clear());

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